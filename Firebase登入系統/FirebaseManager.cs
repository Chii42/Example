using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using System;
using Firebase;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;

[System.Serializable]  // 讓 Unity Inspector 能顯示這個類別
public class FirebaseManager
{
    public FirebaseAuth auth { get; private set; }
    public FirebaseUser user { get; private set; }
    public string Message { get; private set; }
    public event Action<string> OnAuthStateChanged; // 用 event 通知外部（UI Manager 等）

    // Start is called before the first frame update
    public void Initialize()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
    }
    //註冊       帶有 callback
    public void Register(string email, string password, Action<string> onResult)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync 已取消.");
                onResult?.Invoke("註冊已取消");
                return;
            }
            if (task.IsFaulted)
            {
                FirebaseException firebaseEx = task.Exception?.Flatten().InnerException as FirebaseException;
                //var firebaseEx = task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    // 錯誤代碼 (AuthError Enum)
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                    //Message = FirebaseErrorDate.GetErrorMessage(errorCode);
                    onResult?.Invoke(errorCode.ToString());
                }
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("註冊成功");
            }

        });
    }

    //登入
    public void SignIn(string email, string password, Action<string> onResult)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync 已取消.");
                return;
            }
            if (task.IsFaulted)
            {
                FirebaseException firebaseEx = task.Exception?.Flatten().InnerException as FirebaseException;
                if (firebaseEx != null)
                {
                    // 錯誤代碼 (AuthError Enum)
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                    onResult?.Invoke(errorCode.ToString());

                }
                return;
            }
            if (task.IsCompleted)
            {

            }
        });
    }
    public void SignOut()  // 登出
    {
        auth.SignOut();
    }

    public void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            user = auth.CurrentUser;
            if (user != null)
            {
                OnAuthStateChanged?.Invoke("登入");
            }
        }
        if (auth.CurrentUser == null)
        {
            OnAuthStateChanged?.Invoke("登出");
        }
    }
    public void Dispose()
    {
        auth.StateChanged -= AuthStateChanged;
    }

    // 重置密碼
    public void RestPassword(string email)
    {
        string emailAddress = email;
        if (string.IsNullOrEmpty(emailAddress))
        {
            //resetMessageText.text = "請輸入電子郵件地址。";
            return;
        }
        auth.SendPasswordResetEmailAsync(emailAddress).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmailAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SendPasswordResetEmailAsync encountered an error: " + task.Exception);
                return;
            }

            Debug.Log("Password reset email sent successfully.");
        });
    }
}
