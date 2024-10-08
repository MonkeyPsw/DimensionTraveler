using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    //public BlockType blockType;
    public AudioClip BlockBreakSoundClip;
    public GameObject destroyEffect;

    // 이렇게 스크립트 하나에서 나눴어야 했을지도
    //public enum BlockType
    //{
    //    NormalBlock,
    //    CoinBlock,
    //    ItemBlock,

    //}

    void Start()
    {

    }

    void Update()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioSource source = collision.gameObject.GetComponent<AudioSource>();

            foreach (ContactPoint contact in collision.contacts)
            {
                // Player의 윗면과 Cube의 아랫면이 닿았는지 확인
                if (contact.normal.y > 0.9f) // 윗면의 법선 벡터는 (0, 1, 0)에 가깝습니다.
                {
                    // Cube를 없애거나 비활성화하거나 원하는 동작 수행

                    NormalBlockBreak(source);

                    break; // 충돌한 지점 중 하나가 확인되면 반복문을 종료합니다.
                    // 고맙다 CHATGPT
                }
            }

            //Destroy(transform.parent.gameObject);
        }
    }

    void NormalBlockBreak(AudioSource source)
    {
        Destroy(gameObject);

        if (BlockBreakSoundClip != null)
        {
            source.PlayOneShot(BlockBreakSoundClip);
        }

        GameObject effect = Instantiate(destroyEffect, transform.position, Quaternion.identity);
        Destroy(effect, 0.5f);

    }
}
