﻿using System.Collections;
using UnityEngine;
using UnityEngine.AI; // AI, 내비게이션 시스템 관련 코드를 가져오기

// 적 AI를 구현한다
public class Enemy : LivingEntity {
    public LayerMask whatIsTarget; // 추적 대상 레이어

    private LivingEntity targetEntity; // 추적할 대상
    private NavMeshAgent pathFinder; // 경로계산 AI 에이전트

    public ParticleSystem hitEffect; // 피격시 재생할 파티클 효과
    public AudioClip deathSound; // 사망시 재생할 소리
    public AudioClip hitSound; // 피격시 재생할 소리

    private Animator enemyAnimator; // 애니메이터 컴포넌트
    private AudioSource enemyAudioPlayer; // 오디오 소스 컴포넌트
    private Renderer enemyRenderer; // 렌더러 컴포넌트

    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점

    // 추적할 대상이 존재하는지 알려주는 프로퍼티
    private bool hasTarget
    {
        get
        {
            // 추적할 대상이 존재하고, 대상이 사망하지 않았다면 true
            if (targetEntity != null && !targetEntity.dead)
            {
                return true;
            }

            // 그렇지 않다면 false
            return false;
        }
    }

    private void Awake() {
        // 초기화
        pathFinder = GetComponent<NavMeshAgent>();
        enemyAnimator = GetComponent<Animator>();
        enemyAudioPlayer = GetComponent<AudioSource>();

        //렌더러 컴폰넌트는 자식 게임 오브젝트에 있으므로 getcomponentInChildren() 메서드 사용
        enemyRenderer = GetComponentInChildren<Renderer>();

    }

    // 적 AI의 초기 스펙을 결정하는 셋업 메서드
    public void Setup(float newHealth, float newDamage, float newSpeed, Color skinColor) {
        startingHealth = newHealth;
        health = newHealth;
        damage = newDamage;
        pathFinder.speed = newSpeed;
        enemyRenderer.material.color = skinColor;
    }

    private void Start() {
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        StartCoroutine(UpdatePath());
    }

    private void Update() {
        // 추적 대상의 존재 여부에 따라 다른 애니메이션을 재생
        enemyAnimator.SetBool("HasTarget", hasTarget);
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로를 갱신
    private IEnumerator UpdatePath() {
        // 살아있는 동안 무한 루프
        while (!dead)
        {
            if (hasTarget)
            {
                //추적 대상 존재 : 경로를 갱신하고 AI이동을 계속진행
                pathFinder.isStopped = false;
                pathFinder.SetDestination(targetEntity.transform.position);
            }
            else
            {
                //추적 대상 없음 : AI 이동중지
                pathFinder.isStopped = true;
                //Physics.OverlapSphere (위치,생성할 구의 지름 , 레이어 이름)
                //구를 만들어서 구에 겹치는 모든 콜라이더를 배열로 반환
                //모든 콜라이더를 필터링 없이 가져오면 성능낭비가 되므로 레이어 마스크를 입력하여 필터링
                Collider[] colliders = 
                    Physics.OverlapSphere(transform.position,20f,whatIsTarget);

                for (int i =0; i<colliders.Length;i++ )
                {

                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                    //livingEntity 가 notnull 과 죽지않았다면
                    if (livingEntity != null && !livingEntity.dead)
                    {
                        //추적 대상을 해당 livingEntity로 설정
                        targetEntity = livingEntity;
                        break;
                    }
                }
            }
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 데미지를 입었을때 실행할 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) {
        if (!dead)
        {
            hitEffect.transform.position = hitPoint;
            //방향 벡터를 입력받아 해당 방향을 바라보는 쿼터니언 회전값을 반환
            hitEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            hitEffect.Play();
            enemyAudioPlayer.PlayOneShot(hitSound);
        }

        // LivingEntity의 OnDamage()를 실행하여 데미지 적용
        base.OnDamage(damage, hitPoint, hitNormal);
    }

    // 사망 처리
    public override void Die() {
        // LivingEntity의 Die()를 실행하여 기본 사망 처리 실행
        base.Die();

        //다른 ai가 방해받지 않도록 모든 collider를 비활성화
        Collider[] enemyColliders = GetComponents<Collider>();
        for (int i =0; i < enemyColliders.Length; i++)
        {
            enemyColliders[i].enabled = false;
        }

        pathFinder.isStopped = true;
        pathFinder.enabled = false;

        enemyAnimator.SetTrigger("Die");
        enemyAudioPlayer.PlayOneShot(deathSound);
    }

    private void OnTriggerStay(Collider other) {
        // 트리거 충돌한 상대방 게임 오브젝트가 추적 대상이라면 공격 실행   
        if (!dead && Time.time >= lastAttackTime + timeBetAttack)
        {
            LivingEntity attackTarget = other.GetComponent<LivingEntity>();

            if (attackTarget != null && attackTarget ==targetEntity)
            {
                lastAttackTime = Time.time;
                //상대방의 피격위치와 피격방향을 근삿값으로 계산
                //ClosestPoint >> 콜라이더 표면 위의 점중에 특정 위치와 가장 가까운 점을 반환
                //공격 대상 위치에서 자신의 위치로 향하는 방향
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNomal = transform.position - other.transform.position;
                //공격실행
                attackTarget.OnDamage(damage,hitPoint,hitNomal);
            }
        }
    }
}