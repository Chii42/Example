using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

public class UI_Manager : MonoBehaviour
{
    public FirebaseManager firebaseManager;
    public TMP_InputField inputemail;
    public TMP_InputField inputpassword;
    public TMP_InputField inputforgetpassword;
    public GameObject LoginSuccessful;
    public GameObject LoginUi;
    public TMP_Text Errormessage;
    public TMP_Text account;

    Coroutine currentCoroutine;
    // Start is called before the first frame update
    void Awake()
    {
        firebaseManager.Initialize();
        firebaseManager.OnAuthStateChanged += AuthStateChanged;
    }
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Register()
    {
        firebaseManager.Register(inputemail.text, inputpassword.text, (resultMessage) =>
        {
            // 這裡會回傳錯誤訊息
            Debug.Log(resultMessage);
            ShowError(resultMessage);
        });

    }

    public void SignIn()
    {
        firebaseManager.SignIn(inputemail.text, inputpassword.text, (resultMessage) =>
        {
            // 這裡會回傳錯誤訊息
            ShowError(resultMessage);
        });
    }
    public void ShowError(string errorCode)
    {
        LocalizationSettings.StringDatabase.GetLocalizedStringAsync("AuthError", errorCode).Completed += handle =>
        {
            Errormessage.text = handle.Result;
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine=StartCoroutine(CloseError());
        };
    }
    private IEnumerator CloseError()
    {
        yield return new WaitForSeconds(2f);

        Errormessage.text = "";
    }
    public void SignOut()
    {
        firebaseManager.SignOut();
    }
    public void RestPassword()
    {
        firebaseManager.RestPassword(inputforgetpassword.text);
    }

    //帳號狀態切換
    private void AuthStateChanged(string message)
    {
        if (message.StartsWith("登入"))
        {
            account.text = firebaseManager.user.Email;
            LoginSuccessful.SetActive(true);
            LoginUi.SetActive(false);
        }
        else if (message == "登出")
        {
            account.text = "";
            LoginSuccessful.SetActive(false);
            LoginUi.SetActive(true);
        }
    }
   
    void OnDestroy()
    {
        firebaseManager.Dispose(); // 解除事件
    }
}
