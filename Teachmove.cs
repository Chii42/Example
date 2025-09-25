using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Teachmove : MonoBehaviour
{
    CharacterController play;

    float fMoveSpeed = 10f; //移動速度
    float RLSpeed = 7f; //移動速度
    public float gravity = 10;
    public float jumpSpeed;
    bool jump = false;
    private Vector3 movement;

    float runSpeed = 28f; //衝刺速度

    public float displayTime = 2;
    private float timerDisplay = -1;
    bool movdie=false;

    public Image hp;
    public Image hp2;   //緩慢減少的血
    public TextMeshProUGUI text;  //顯示血量數值
    float i;

    public Vgtb vgt;  //吃到道具
    public Damage d;  //受傷

    public Animator ani1;  //馬
    public Animator ani2;  //花椰菜
    public GameObject ani3;  //花椰菜骨架
    public GameObject effect;  //特效
    public GameObject runeff;  //跑步特效
    public Image die;  //死亡

    public camera end;  //結束

    public AudioClip Audio;
    public AudioClip Audio2;
    public AudioSource audiosource;

    public Animator aniui;  //UI
    public ParticleSystem hpeff;  //回血特效
    float creff=3 ;

    public ParticleSystem fallhp;  //扣血特效


    // Start is called before the first frame update
    void Start()
    {
        play = GetComponent<CharacterController>();
        vgt = FindObjectOfType<Vgtb>();
        d = GetComponent<Damage>();
        ani2.enabled = false;   //關閉動畫
        hpeff.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        ability.currenthp = Mathf.Clamp(ability.currenthp, 0, ability.maxhp);  //限制最大值
        fMoveSpeed = Mathf.Clamp(fMoveSpeed, 10, runSpeed);  //限制最大值
        RLSpeed = Mathf.Clamp(RLSpeed, RLSpeed, RLSpeed);
        hp.fillAmount = ability.currenthp / ability.maxhp;
        text.text = ability.currenthp.ToString();


        if (hp2.fillAmount!= hp.fillAmount)
        {
            hp2.fillAmount = Mathf.Lerp(hp2.fillAmount, hp.fillAmount, 2*Time.deltaTime);
        }


        if (play.isGrounded)
        {
            if(movdie == false)
            {
                runeff.SetActive(true);
                if (audiosource.isPlaying == false)
                {
                    audiosource.PlayOneShot(Audio);
                }
            }
            movement = new Vector3(i * RLSpeed, 0, 1 * fMoveSpeed);
            movement = transform.TransformDirection(movement);
            if (jump)
            {
                audiosource.Stop();
                runeff.SetActive(false);
                movement.y = jumpSpeed;
                jump = false;
            }
        }
        movement.y -= gravity * Time.deltaTime;  //重力往下掉

        if (movdie == false)
        {
            play.Move(movement * Time.deltaTime); //沒有撞到物體往前移動   
        }

        if (fMoveSpeed>10)    //加速後的減速效果
        {
            fMoveSpeed = fMoveSpeed - Time.deltaTime * 15;
        }
       
        if (Input.GetKey(KeyCode.D))
        {
            hide(1);    
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpcan();
        }
        if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A))
        {
            hide(0);
        }
        if (Input.GetKey(KeyCode.A))
        {
            hide(-1);
        }

        if(ability.currenthp==0)
        {
            movdie = true;
            die.gameObject.SetActive(true);
            StartCoroutine(Wat());
           
        }

        if (timerDisplay >= 0)
        {
            timerDisplay -= Time.deltaTime;
            if (timerDisplay < 0)
            {
                movdie = false;
                ani2.enabled = false;
                ani3.SetActive(true);   
                ani1.SetBool("hit", false);
            }
        }

        hpeff.transform.position = transform.position;

        if(creff<2)
        {
            creff += Time.deltaTime;
            if (creff >=2)
            {
                hpeff.Stop();
            }
        }


    }
    IEnumerator Wat()   //等
    {
        yield return new WaitForSeconds(3f);
        KinectManager manager = KinectManager.Instance;
        if (manager && KinectManager.IsKinectInitialized())
        {
            ability.currenthp = ability.maxhp;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    public void hide(float val)
    {
        if(movdie == false)
        {
            float x = val;
            i = x;
        }
    }
    public void jumpcan()
    {
        if(jump==false)
        {
            jump = true;
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag== "Obstacle")
        {
            if(fMoveSpeed == 10)
            {
                runeff.SetActive(false);
                ani2.enabled = true;
                ani1.SetBool("hit", true);
                ani3.SetActive(false);
                timerDisplay = displayTime;
                Destroy(other.gameObject);
                movdie = true;
                ability.currenthp -= 8;
                audiosource.Stop();
                audiosource.PlayOneShot(Audio2);
            }
            else
            {
                Instantiate(effect, other.transform.position, other.transform.rotation);
                Destroy(other.gameObject);
            }
           
        }

        if (other.tag == "run")
        {
            fMoveSpeed = runSpeed;           
        }

        if (other.tag == "VGTB")
        {
            Destroy(other.gameObject);
            vgt.v++;
        }

        if (other.tag == "apple")
        {
            if(ability.currenthp==ability.maxhp)
            {
                Destroy(other.gameObject);
                vgt.v++;      
            }
            else
            {
                Destroy(other.gameObject);
                ability.currenthp += 4;
                hpeff.Play();
                creff = 0;
            }
            
        }

        if (other.tag == "tea")
        {
            Destroy(other.gameObject);
            int a = Random.Range(1, 4);
            vgt.v = vgt.v - a;
            ability.currenthp -= 15;
            d.flashScreen();
            Instantiate(fallhp, other.transform.position, other.transform.rotation);
        }

        if (other.tag == "road")
        {
            end.Cakeani.SetBool("end",true);
            end.cin.enabled = false;
            end.cameraMov = false;
            end.i ++;
        }

        if (other.name == "boss")
        {
            Next();
        }

        if (other.name == "ui1")
        {
            aniui.SetBool("ui1", true);
        }

        if (other.name == "ui2")
        {
            aniui.SetBool("ui2", true);
        }

        if (other.name == "ui3")
        {
            aniui.SetBool("ui3", true);
        }
    }

    public void Next()
    {
        KinectManager manager = KinectManager.Instance;
        if (manager && KinectManager.IsKinectInitialized())
        {
            float x = Mathf.Round(vgt.v / 5);  //道具四捨五入換成能力值
            ability.att += (int)x;
            SceneManager.LoadScene(4);
        }

    }

}
