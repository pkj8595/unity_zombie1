using System.Collections;
using UnityEngine;

// 총을 구현한다
public class Gun : MonoBehaviour {
    // 총의 상태를 표현하는데 사용할 타입을 선언한다
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magCapacity = 25; // 탄창 용량
    public int magAmmo; // 현재 탄창에 남아있는 탄약


    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간
    private float lastFireTime; // 총을 마지막으로 발사한 시점


    private void Awake() {
        // 사용할 컴포넌트들의 참조를 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();
        //사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;
        //라인렌더러 비활성화
        bulletLineRenderer.enabled = false;

    }
    // 총 상태 초기화
    private void OnEnable() {
        //탄창채우기
        magAmmo = magCapacity;
        //총쏠 준비가 된 상태로 변경
        state = State.Ready;
        //마지막총을 쏜 시점을 초기화
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire() {
        if (state == State.Ready && Time.time >= lastFireTime + timeBetFire)
        {
            lastFireTime = Time.time;
            Shot();
        }
    }

    // 실제 발사 처리
    private void Shot() {
        //충돌정보 컨테이너
        RaycastHit hit;
        //탄알을 맞은 곳을 저장할 변수
        Vector3 hitPosition = Vector3.zero;

        //레이케스트(시작지점,방향,충돌 정보 컨테이너, 사정거리)
        if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            //레이가 어떤 물체와 충돌한 경우

            IDamageable target = hit.collider.GetComponent<IDamageable>();
            //상대방으로부터 IDamageable 오브젝트를 가져오는데 성공했다면

            if (target != null)
            {
                //상대방으로부터 OnDamage 함수를 실행시켜 상대방에 대미지 주기
                target.OnDamage(damage, hit.point, hit.normal);
            }
            //레이가 충돌한 위치 저장
            hitPosition = hit.point;
        }
        else
        {
            //레이가 다른 물체와 충돌하지 않았다면 
            //탄알이 최대 사정거리까지 날아갔을 때의 위치를 충돌위치로 사용
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }
        //발사 이펙트 재생
        StartCoroutine(ShotEffect(hitPosition));
        magAmmo--;
        if(magAmmo <= 0)
        {
            //탄창에 총알이 없다면 총의 상태를 empty로 갱신
            state = State.Empty;
        }

    }

    // 발사 이펙트와 소리를 재생하고 총알 궤적을 그린다
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        //총구화염 효과
        muzzleFlashEffect.Play();
        //탄피배출 효과
        shellEjectEffect.Play();
        //총격 소리 재생
        gunAudioPlayer.PlayOneShot(shotClip);
        //선의 시작점은 총구의 위치 
        bulletLineRenderer.SetPosition(0,fireTransform.position);
        //선의 끝점은 입력으로 들어온 충돌 위치
        bulletLineRenderer.SetPosition(1, hitPosition);

        // 라인 렌더러를 활성화하여 총알 궤적을 그린다
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 총알 궤적을 지운다
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if (state == State.Reloading || ammoRemain <=0 || magAmmo >= magCapacity)
        {
            Debug.Log("Gun.Relad : reture false");
            Debug.Log("state : "+state);
            Debug.Log("ammoRemain : " + ammoRemain);
            Debug.Log("magAmmo : " + magAmmo);
            Debug.Log("magCapacity : " + magCapacity);
            //이미 재장전 중이거나 남은 탄알이 없거나 탄창에 탄알이 이미 가능한 경우 재장전 x
            return false;
        }
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        //재장전 소리 재생
        gunAudioPlayer.PlayOneShot(reloadClip);
        
        // 재장전 소요 시간 만큼 처리를 쉬기
        yield return new WaitForSeconds(reloadTime);

        // 탄창에 채워야 할 탄알 수 계산
        int ammoToFill = magCapacity - magAmmo;

        //탄창에 채워야 할 탄알이 남은 탄알보다 많다면
        //채워야 할 탄알 수를 남은 탄알 수에 맞춰 줄임.
        if (ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }
        //탄창을채움
        magAmmo += ammoToFill;
        //남은 탄알에서 탄창에 채운만큼 탄알을 뺌
        ammoRemain -= ammoToFill;
        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}