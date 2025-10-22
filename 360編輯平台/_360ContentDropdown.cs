using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Firebase.Database;
using Firebase.Storage;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class _360ContentDropdown : MonoBehaviour
{
       
    string authorPath;                        // Firebase資料庫路徑
    private StorageReference firebaseStorageURL;
    [Header("書頁的資訊")]
    public string storyBookNumber;            // 書本編號
    public string databaseSort;               // 書頁
    public int databaseOrder = -1;          //按鈕編號排序
    public int targetpage;           //目標頁面排序

    public RectTransform loadingPanel;              // 載入面板 
    public RectTransform loadingPanelInProgress;    // 編輯時的載入面板
    
    //------------------------------------------------------------
    [Header("編輯面板的資訊")]
    public TMP_Dropdown dropdown; // 綁定你的 Dropdown UI
    public RectTransform[] EditPanel;
    public TMP_InputField textEdit;    // 文字內容

    //圖片面板的資訊
    private bool isClicked;                            // 是否點擊
    public Image selectedImage;                    // 選中圖片預覽
    private Texture2D imageTexture2D;               // 圖片暫存
    string processedUrl;                         // 圖片路徑
    Sprite image;

    //連結書頁的資訊
    public TMP_InputField LinkPage;

    public RectTransform ContentEditingPanel;         // 360內容編輯面板
    public GameObject buttonTemplate;              // 版型按鈕模板
    public GameObject layoutPanelContent;           // 按鈕面板的欄位
    public GameObject deletebutton;           // 刪除的按鈕
    public string LayoutType;  // 版面類型(圖片或影片)
    string typeList ;  // 類型
    public TMP_InputField[] TimeSetting;            // 時間欄位
    public TMP_InputField[] PosSetting;             // 座標欄位
    string PosZ;
    public TMP_InputField titleSetting;             // 標題欄位
    public GameObject buttonPrefab;
    public Transform parentObject;                // 放新增內容的父物件
    public GameObject camera360;                  // 瀏覽環景的攝影機
    Vector3 centerWorldPos;                       //RawImage 中央的世界位置（攝影機正前方）
    float distanceFromCamera = 70f; // 可根據場景調整距離
    bool NewButton=true;
    public VideoProgressBar videoProgressBar;
    [Header("新增的內容資訊")]
    public int currentEditingButton ;
    public List<GameObject> buttonTemplateList = new List<GameObject>();
    public List<GameObject> buttonobjectList = new List<GameObject>();
    public List<string> buttonOrder = new List<string>();      // 互動內容順序

    private BookPagesEditControl bookPagesEditCtrl;  // 書頁編輯控制器
    ToggleDropdownControl toggleDropdownControl;
    DontDestroy dontDestroy;
    public bool CanMoveCamera = true;

    void Awake()
    {
        dontDestroy = GameObject.Find("DontDestroy").gameObject.GetComponent<DontDestroy>();            
    }
    

    void Start()
    {
        authorPath = dontDestroy.authorName;
        firebaseStorageURL = FirebaseStorage.DefaultInstance.GetReferenceFromUrl(dontDestroy.storageLink);
        bookPagesEditCtrl = GameObject.Find("GC").GetComponent<BookPagesEditControl>();
        toggleDropdownControl= FindObjectOfType<ToggleDropdownControl>();
        // 設定初始值
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
               
    }

    public void OnDropdownValueChanged(int index)
    {
        // 根據選擇的索引來執行不同的動作
        switch (index)
        {
            case 0:
                typeList = "文字";
                break;
            case 1:
                typeList = "圖片";
                break;
            case 2:
                typeList = "連結書頁";
                break;
            case 3:
                typeList = "互動物件";
                distanceFromCamera = 1.5f;
                ShowEditPanel(index);
                return; // 提前跳出，避免重複設定距離
        }

        // 對於大多數選項的共同距離設定
        distanceFromCamera = 70f;
        ShowEditPanel(index);   
    }
    public void ShowEditPanel(int Panelindex)      // 切換顯示的版面
    {
        CanMoveCamera = false;
        toggleDropdownControl.PopulateDropdown();       // 更新互動液體選單內容
        videoProgressBar.PauseVideo();
        for (int i = 0; i < EditPanel.Length; i++)
        {
            EditPanel[i].gameObject.GetComponent<UIStartPosition>()._CI();
        }
        EditPanel[Panelindex].anchoredPosition = Vector2.zero;

        // ====== 自動設定座標在 RawImage 畫面中央 ======

        // 第二攝影機畫面中心的世界座標 = 攝影機位置 + forward * 距離
        centerWorldPos = camera360.transform.position + camera360.transform.forward * distanceFromCamera;

        PosSetting[0].text = centerWorldPos.x.ToString("F2");
        PosSetting[1].text = centerWorldPos.y.ToString("F2");
        PosZ = centerWorldPos.z.ToString("F2");

        if (LayoutType == "環景圖片")
        {
            for (int i = 0; i < TimeSetting.Length; i++)
            {
                TimeSetting[i].interactable = false;
            }
            TimeSetting[0].text = "00:00";
            TimeSetting[1].text = "0";
        }
        else
        {
            TimeSetting[0].text = "00:00";
            TimeSetting[1].interactable = true;
        }
    }
    public void Add360Content()
    {
        if (LayoutType !="")
        {
            ContentEditingPanel.anchoredPosition = Vector2.zero;
            // 手動執行一次，確保預設選項能觸發事件
            ResetDropdownToFirst();
        }

    }
    void ResetDropdownToFirst()
    {
        Cleareditdata();
        dropdown.value = 0;  // 選到第一個選項
        dropdown.onValueChanged.Invoke(0);  // 手動觸發事件
    }
    public void ConfirmAdd()
    {
        toggleDropdownControl.OnFinishClicked();

        if (NewButton)
        {
            // 處理資料庫排序
            if (buttonOrder.Count == 0)                       // 如果目前 buttonOrder 沒有任何資料
            {
                databaseOrder = 0;                            // 設定第一筆資料的排序為 0
                buttonOrder.Add(databaseOrder.ToString());    // 將排序數字轉成字串後加入 buttonOrder 清單中
            }
            else if (buttonOrder.Count > 0)
            {
                int maxOrder = int.MinValue;                 // 先將最大排序初始化為 int 的最小值，方便之後比較

                foreach (var order in buttonOrder)           // 迭代目前所有的排序資料
                {
                    int currentOrder = int.Parse(order);     // 將字串型態的排序轉換成整數
                    if (currentOrder > maxOrder)             // 比較目前的排序是否大於目前已知的最大值
                    {
                        maxOrder = currentOrder;             // 更新最大排序值
                    }
                }

                databaseOrder = maxOrder + 1;                // 新資料的排序為目前最大排序 + 1，確保排序不重複且遞增
                buttonOrder.Add(databaseOrder.ToString());   // 將新的排序轉成字串後加入 buttonOrder 清單中
            }
            // 建立 InteractionData 資料
            InteractionManager.InteractionData newData = new InteractionManager.InteractionData
            {
                X座標 = PosSetting[0].text,
                Y座標 = PosSetting[1].text,
                Z座標 = PosZ,
                標題 = titleSetting.text,
                開始時間 = TimeSetting[0].text,
                持續時間 = TimeSetting[1].text,
                類型 = typeList,
                按鈕編號 = databaseOrder,
            };
            if (typeList == "文字")
            {
                newData.內容 = textEdit.text;
            }
            else if (typeList == "圖片")
            {
                newData.image = image;
            }
            else if (typeList == "連結書頁")
            {
                newData.內容 = LinkPage.text;
            }
            else if (typeList == "互動物件")
            {
                newData.內容 = toggleDropdownControl.otherObjectValue;
                newData.其他類別 = toggleDropdownControl.otherObjectType;
                newData.容器 = toggleDropdownControl.containerValue;
                newData.容器尺寸 = toggleDropdownControl.containerSize;
                newData.液體 = toggleDropdownControl.liquidType;
                newData.液體容量 = toggleDropdownControl.liquidValue;
                newData.互動液體 = toggleDropdownControl.Interactiveliquid[0];
                newData.互動液體2 = toggleDropdownControl.Interactiveliquid[1];
                newData.互動順序 = toggleDropdownControl.sort;
            }


            // ===> 建立 Interactive Button
            GameObject interactiveButton = Instantiate(buttonPrefab);
            interactiveButton.transform.SetParent(parentObject.transform, false);
            interactiveButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            interactiveButton.transform.localPosition = centerWorldPos;

            var button = interactiveButton.GetComponent<InteractiveButton>();
            ApplyInteractionDataToButton(button, newData);
            buttonobjectList.Add(interactiveButton);

            // ===> 建立 Version Thumbnail
            GameObject versionContainer = Instantiate(buttonTemplate);
            versionContainer.transform.SetParent(layoutPanelContent.transform, false);
            versionContainer.transform.localScale = Vector3.one;

            var thumbnail = versionContainer.GetComponent<ButtonThumbnail>();
            ApplyInteractionDataToThumbnail(thumbnail, newData);
            thumbnail.ButtonSort = buttonobjectList.Count - 1;
            thumbnail.number = databaseOrder;
            buttonTemplateList.Add(versionContainer);

            // ===> 同步新增到對應 InteractionManager
            bookPagesEditCtrl.thumbnailList[targetpage].GetComponent<InteractionManager>().interactionList.Add(newData);

        }
        else
        {
            //更新內容

            // 取得要更新的資料
            var interactionData = bookPagesEditCtrl.thumbnailList[targetpage].GetComponent<InteractionManager>().interactionList[currentEditingButton];
            // 更新 InteractionData 內容
            interactionData.X座標 = PosSetting[0].text;
            interactionData.Y座標 = PosSetting[1].text;
            interactionData.Z座標 = PosZ;
            interactionData.標題 = titleSetting.text;
            interactionData.開始時間 = TimeSetting[0].text;
            interactionData.持續時間 = TimeSetting[1].text;
            interactionData.類型 = typeList;
            if (typeList == "文字")
            {
                interactionData.內容 = textEdit.text;
            }
            else if (typeList == "圖片")
            {
                if (image == null)
                {
                    //沒變圖片
                }
                else
                {
                    interactionData.image = image;
                }
            }
            else if (typeList == "連結書頁")
            {
                interactionData.內容 = LinkPage.text;
            }
            else if (typeList == "互動物件")
            {
                //
                interactionData.內容 = toggleDropdownControl.otherObjectValue;
                interactionData.其他類別 = toggleDropdownControl.otherObjectType;
                interactionData.容器 = toggleDropdownControl.containerValue;
                interactionData.容器尺寸 = toggleDropdownControl.containerSize;
                interactionData.液體 = toggleDropdownControl.liquidType;
                interactionData.液體容量 = toggleDropdownControl.liquidValue;
                interactionData.互動液體 = toggleDropdownControl.Interactiveliquid[0];
                interactionData.互動液體2 = toggleDropdownControl.Interactiveliquid[1];
                interactionData.互動順序 = toggleDropdownControl.sort;
            }

            // ===> 套用更新到 互動按鈕
            GameObject interactiveButton = buttonobjectList[currentEditingButton];
            interactiveButton.transform.localPosition = centerWorldPos;
            var button = interactiveButton.GetComponent<InteractiveButton>();
            ApplyInteractionDataToButton(button, interactionData);
            button.texttonumber();
            button.creatprefab();

            // ===> 套用更新到 縮圖
            GameObject versionContainer = buttonTemplateList[currentEditingButton];
            var thumbnail = versionContainer.GetComponent<ButtonThumbnail>();
            ApplyInteractionDataToThumbnail(thumbnail, interactionData);

        }

        UploadInteractiveData();
        ContentEditingPanel.gameObject.GetComponent<UIStartPosition>()._CI();
        Cleareditdata();
        NewButton = true;
        deletebutton.SetActive(false);
        CanMoveCamera = true;    
    }
    void ApplyInteractionDataToButton(InteractiveButton button, InteractionManager.InteractionData data)
    {
        button.titletext.text = data.標題;
        button.type = data.類型;
        button.TimeStart = data.開始時間;
        button.EndTime = data.持續時間;

        if (data.類型 == "文字")
        {
            button.content = data.內容;
        }
        else if (data.類型 == "圖片")
        {
            button.image = data.image;
        }
        else if (data.類型 == "連結書頁")
        {
            button.content = data.內容;
        }
        else if (data.類型 == "互動物件")
        {
            button.otherObjectType = data.其他類別;
            button.content = data.內容;
            button.container = data.容器;
            button.containerSize = data.容器尺寸;
            button.liquid = data.液體;
            button.liquidValue = data.液體容量;
            button.Interactiveliquid[0] = data.互動液體;
            button.Interactiveliquid[1] = data.互動液體2;
            button.sort = data.互動順序;
        }
    }

    void ApplyInteractionDataToThumbnail(ButtonThumbnail thumbnail, InteractionManager.InteractionData data)
    {
        thumbnail.title.text = data.標題;
        thumbnail.PosX = data.X座標;
        thumbnail.PosY = data.Y座標;
        thumbnail.TimeStart = data.開始時間;
        thumbnail.EndTime = data.持續時間;
        thumbnail.type = data.類型;

        if (data.類型 == "文字")
        {
            thumbnail.content = data.內容;
        }
        else if (data.類型 == "圖片")
        {
            thumbnail.image = data.image;
        }
        else if (data.類型 == "連結書頁")
        {
            thumbnail.content = data.內容;
        }
        else if (data.類型 == "互動物件")
        {
            thumbnail.content = data.內容;
            thumbnail.otherObjectType = data.其他類別;
            thumbnail.container = data.容器;
            thumbnail.containerSize = data.容器尺寸;
            thumbnail.liquid = data.液體;
            thumbnail.liquidValue = data.液體容量;
            thumbnail.Interactiveliquid[0] = data.互動液體;
            thumbnail.Interactiveliquid[1] = data.互動液體2;
            thumbnail.sort = data.互動順序;
        }
    }
    public void CancelAdd()  //取消編輯
    {
        //回復面板位置
        ContentEditingPanel.gameObject.GetComponent<UIStartPosition>()._CI();
        for (int i = 0; i < EditPanel.Length; i++)
        {
            EditPanel[i].gameObject.GetComponent<UIStartPosition>()._CI();
        }
        Cleareditdata();
        NewButton = true;
        deletebutton.SetActive(false);
        CanMoveCamera = true; 
    }
    public void DownloadFireBaseDate(List<InteractionManager.InteractionData> dataList)
    {
        foreach (var data in dataList)
        {
            // 這裡你就可以建立互動按鈕
            CreateInteractiveFromData(data);
        }

    }
    //讀取firseBase上的資料
    private void CreateInteractiveFromData(InteractionManager.InteractionData data)
    {
        // 實作你的互動按鈕產生邏輯（你之前寫的那段 Instantiate 內容）
        Debug.Log("產生互動內容: " + data.標題);
        // 取得位置（X、Y 字串轉 float）
        float x = float.Parse(data.X座標);
        float y = float.Parse(data.Y座標);
        float z = float.Parse(data.Z座標);

        Vector3 centerWorldPos = new Vector3(x, y, z);

        // ======= 建立按鈕縮圖 =======
        GameObject versionContainer = Instantiate(buttonTemplate);
        versionContainer.transform.SetParent(layoutPanelContent.transform, false);
        versionContainer.transform.localScale = Vector3.one;
        buttonTemplateList.Add(versionContainer);

        // ======= 建立互動按鈕 =======
        GameObject interactiveButton = Instantiate(buttonPrefab);
        interactiveButton.transform.SetParent(parentObject.transform, false);
        interactiveButton.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        interactiveButton.transform.localPosition = centerWorldPos;

        var buttonComponent = interactiveButton.GetComponent<InteractiveButton>();
        buttonComponent.titletext.text = data.標題;
        buttonComponent.type = data.類型;
        buttonComponent.TimeStart = data.開始時間;
        buttonComponent.EndTime = data.持續時間;

        if (data.類型 == "文字")
        {
            buttonComponent.content = data.內容;
        }
        else if (data.類型 == "圖片")
        {
            // TODO：圖片內容該怎麼處理？是 URL？還是 base64？ => 可補這部分
            if (data.image != null)
            {
                // 直接使用快取的 Sprite
                buttonComponent.image = data.image;
            }
            else
            {
                // 還沒載入，先下載然後存進去
                StartCoroutine(LoadImageFromURL(data.內容, sprite =>
                {
                    if (sprite != null)
                    {
                        data.image = sprite; // 存進資料裡
                        buttonComponent.image = sprite;
                    }
                }));
            }
        }
        else if (data.類型 == "連結書頁")
        {
            buttonComponent.content = data.內容;
        }
        else if (data.類型 == "互動物件")
        {
            buttonComponent.content = data.內容;
            buttonComponent.container = data.容器;
            buttonComponent.liquid = data.液體;
            buttonComponent.sort = data.互動順序;

            buttonComponent.otherObjectType = data.其他類別;
            buttonComponent.containerSize = data.容器尺寸;
            buttonComponent.liquidValue = data.液體容量;
            buttonComponent.Interactiveliquid[0] = data.互動液體;
            buttonComponent.Interactiveliquid[1] = data.互動液體2;
        }

        buttonobjectList.Add(interactiveButton);
        buttonOrder.Add(data.按鈕編號.ToString());

        // ======= 儲存資訊到縮圖 =======
        int currentIndex = buttonobjectList.Count - 1;
        var thumbnail = versionContainer.GetComponent<ButtonThumbnail>();
        thumbnail.title.text = data.標題;
        thumbnail.PosX = data.X座標;
        thumbnail.PosY = data.Y座標;
        thumbnail.TimeStart = data.開始時間;
        thumbnail.EndTime = data.持續時間;
        thumbnail.type = data.類型;

        if (data.類型 == "文字")
        {
            thumbnail.content = data.內容;
        }
        else if (data.類型 == "圖片")
        {
            // 同樣：根據資料來源處理圖片
            if (data.image != null)
            {
                // 直接使用快取的 Sprite
                thumbnail.image = data.image;
            }
            else
            {
                // 還沒載入，先下載然後存進去
                StartCoroutine(LoadImageFromURL(data.內容, sprite =>
                {
                    if (sprite != null)
                    {
                        data.image = sprite; // 存進資料裡
                        thumbnail.image = sprite;
                    }
                }));
            }
        }
        else if (data.類型 == "連結書頁")
        {
            thumbnail.content = data.內容;
        }
        else if (data.類型 == "互動物件")
        {
            thumbnail.content = data.內容;
            thumbnail.otherObjectType = data.其他類別;
            thumbnail.container = data.容器;
            thumbnail.containerSize = data.容器尺寸;
            thumbnail.liquid = data.液體;
            thumbnail.liquidValue = data.液體容量;
            thumbnail.Interactiveliquid[0] = data.互動液體;
            thumbnail.Interactiveliquid[1] = data.互動液體2;
            thumbnail.sort = data.互動順序;
        }

        thumbnail.ButtonSort = currentIndex;
        thumbnail.number = data.按鈕編號;
    }
    //從FireBase載圖片
    IEnumerator LoadImageFromURL(string url, System.Action<Sprite> callback)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite sprite = Sprite.Create(texture, rect, pivot);
            callback?.Invoke(sprite);
        }
        else
        {
            Debug.LogError("圖片載入失敗：" + request.error);
            callback?.Invoke(null);
        }
    }
    void Update()
    {
       
    }
    
    void Cleareditdata()   //清空面板資料
    {
        for (int i = 0; i < TimeSetting.Length; i++)
        {
            TimeSetting[i].text = "";
        }
        for (int i = 0; i < PosSetting.Length; i++)
        {
            PosSetting[i].text = "";
        }
        titleSetting.text= "";

        // 清空文字數據
        textEdit.text="";
        LinkPage.text="";

        // 清空圖片數據
        Imagedisplay(selectedImage, false);
        imageTexture2D = null; 
        image= null; 

        // 清空互動模型數據
        toggleDropdownControl.ClearForm();             

    }

    // 刪除所有按鈕
    public void ClearAllButtons()
    {
        // 刪除所有按鈕
        foreach (var btn in buttonobjectList)
        {
            Destroy(btn);
        }
        buttonobjectList.Clear();

        // 刪除所有按鈕縮圖
        foreach (var btn in buttonTemplateList)
        {
            Destroy(btn);
        }
        buttonTemplateList.Clear();


        buttonOrder.Clear();
        
    }

    // 刪除特定按鈕（例如第 2 個）
    public void RemoveButtonAt()
    {
        // 刪除特定按鈕
        Destroy(buttonobjectList[currentEditingButton]);
        buttonobjectList.RemoveAt(currentEditingButton);

        // 刪除特定按鈕縮圖
        Destroy(buttonTemplateList[currentEditingButton]);
        buttonTemplateList.RemoveAt(currentEditingButton);

        bookPagesEditCtrl.thumbnailList[targetpage].GetComponent<InteractionManager>().interactionList.RemoveAt(currentEditingButton);


        string path = authorPath + "/書庫/" + storyBookNumber + "/書頁/" + databaseSort + "/360互動內容/";
        DatabaseReference Data = FirebaseDatabase.DefaultInstance.GetReference(path);
        Data.Child(databaseOrder.ToString()).SetValueAsync(null);

        // 刪除相關的存儲文件
        StorageReference Storage = firebaseStorageURL.Child(path);
        Storage.Child(databaseOrder.ToString()).Child("圖片.png").DeleteAsync();
        
        // ===> 重排剩下按鈕和縮圖的編號
        for (int i = 0; i < buttonTemplateList.Count; i++)
        {
            // 更新縮圖的 ButtonSort
            var thumbnail = buttonTemplateList[i].GetComponent<ButtonThumbnail>();
            thumbnail.ButtonSort = i;
        }

        CancelAdd();          
    }

    // 刪除FireBase存的圖片
    public void DeleteAll360Data(int storyBookNumber,string pageSort)
    {
        string path = authorPath + "/書庫/" + storyBookNumber + "/書頁/" + pageSort + "/360互動內容/";
        for (int i = 0; i < 30; i++)
        {
            int currentIndex = i; // 把 i 存到區域變數中
            // 刪除 Storage 圖片
            StorageReference imgRef = firebaseStorageURL.Child(path).Child(i.ToString()).Child("圖片.png");

            imgRef.DeleteAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                    Debug.Log($" 圖片 {currentIndex} 刪除成功！");
                else
                    Debug.LogError($" 圖片 {currentIndex} 刪除失敗：{task.Exception}");
            });
        }
    }

    // 編輯已經有的內容
    public void StartEditingFromThumbnail(ButtonThumbnail thumbnail = null)
    {
        toggleDropdownControl.ClearForm();
        if (thumbnail != null)
        {
            // 編輯模式
            int sortIndex = thumbnail.ButtonSort;

            if (sortIndex >= 0 && sortIndex < buttonobjectList.Count)
            {
                var button = buttonobjectList[sortIndex];
                if (button != null)
                {
                    Cleareditdata();
                    deletebutton.SetActive(true);

                    //Debug.Log($"準備編輯按鈕：{button.name}");
                    currentEditingButton = sortIndex;
                 
                    // 將資料塞入 UI 編輯欄位
                    titleSetting.text = thumbnail.title.text;
                    TimeSetting[0].text = thumbnail.TimeStart;
                    TimeSetting[1].text = thumbnail.EndTime;
                    typeList = thumbnail.type;
                    databaseOrder = thumbnail.number;
                    if (typeList == "文字")
                    {
                        textEdit.text=thumbnail.content;
                        dropdown.value = 0;  // 選到第一個選項
                        dropdown.onValueChanged.Invoke(0);  // 手動觸發事件
                    }
                    else if (typeList == "圖片")
                    {
                        selectedImage.sprite = thumbnail.image;
                        Imagedisplay(selectedImage, true);
                        dropdown.value = 1;  // 選到第一個選項
                        dropdown.onValueChanged.Invoke(1);  // 手動觸發事件
                    }
                    else if (typeList == "連結書頁")
                    {
                        LinkPage.text=thumbnail.content;
                        dropdown.value = 2;  // 選到第一個選項
                        dropdown.onValueChanged.Invoke(2);  // 手動觸發事件
                    }
                    else if (typeList == "互動物件")
                    {
                        dropdown.value = 3;  // 選到第一個選項
                        dropdown.onValueChanged.Invoke(3);  // 手動觸發事件
                        toggleDropdownControl.RestoreUIFromSavedData(thumbnail.otherObjectType,thumbnail.content,thumbnail.container,thumbnail.containerSize,thumbnail.liquid,thumbnail.liquidValue,thumbnail.Interactiveliquid,thumbnail.sort);
                    }

                    NewButton =false;
                }
                else
                {
                    Debug.LogWarning("buttonobjectList 裡的對應項目是 null！");
                }
            }
            else
            {
                Debug.LogWarning("ButtonSort 超出範圍，找不到對應按鈕！");
            }
        }
        else
        {
            // 新增模式
            Debug.Log("開始新增按鈕");
            NewButton=true;
        }
    }

    public void UploadInteractiveData()  //上傳互動內容資料
    {    
        string path = authorPath + "/書庫/" + storyBookNumber + "/書頁/" + databaseSort+ "/360互動內容/";
        DatabaseReference Data = FirebaseDatabase.DefaultInstance.GetReference(path);

        Data.Child(databaseOrder.ToString()).Child("類型").SetValueAsync(SafeText(typeList));
        Data.Child(databaseOrder.ToString()).Child("標題").SetValueAsync(SafeText(titleSetting.text));
        Data.Child(databaseOrder.ToString()).Child("X座標").SetValueAsync(SafeText(PosSetting[0].text));
        Data.Child(databaseOrder.ToString()).Child("Y座標").SetValueAsync(SafeText(PosSetting[1].text));
        Data.Child(databaseOrder.ToString()).Child("Z座標").SetValueAsync(SafeText(PosZ));
        Data.Child(databaseOrder.ToString()).Child("開始時間").SetValueAsync(SafeText(TimeSetting[0].text));
        Data.Child(databaseOrder.ToString()).Child("持續時間").SetValueAsync(SafeText(TimeSetting[1].text));
        Data.Child(databaseOrder.ToString()).Child("按鈕編號").SetValueAsync(databaseOrder);
        if (typeList == "文字")
        {
            Data.Child(databaseOrder.ToString()).Child("內容").SetValueAsync(SafeText(textEdit.text));
        }
        else if (typeList == "圖片")
        {
            if(image==null)
            {
                //沒變圖片
            }else
            {
                StartCoroutine(UploadImageCoroutine("圖片"));
            }
            
        }
        else if (typeList == "連結書頁")
        {
            Data.Child(databaseOrder.ToString()).Child("內容").SetValueAsync(SafeText(LinkPage.text));
        }
        else if (typeList == "互動物件")
        {
            Data.Child(databaseOrder.ToString()).Child("內容").SetValueAsync(toggleDropdownControl.otherObjectValue);
            Data.Child(databaseOrder.ToString()).Child("其他類別").SetValueAsync(toggleDropdownControl.otherObjectType);
            Data.Child(databaseOrder.ToString()).Child("容器").SetValueAsync(toggleDropdownControl.containerValue);
            Data.Child(databaseOrder.ToString()).Child("容器尺寸").SetValueAsync(toggleDropdownControl.containerSize);
            Data.Child(databaseOrder.ToString()).Child("液體").SetValueAsync(toggleDropdownControl.liquidType);
            Data.Child(databaseOrder.ToString()).Child("液體容量").SetValueAsync(toggleDropdownControl.liquidValue);
            Data.Child(databaseOrder.ToString()).Child("互動液體").SetValueAsync(toggleDropdownControl.Interactiveliquid[0]);
            Data.Child(databaseOrder.ToString()).Child("互動液體2").SetValueAsync(toggleDropdownControl.Interactiveliquid[1]);
            Data.Child(databaseOrder.ToString()).Child("互動順序").SetValueAsync(toggleDropdownControl.sort);
        }
    }
    string SafeText(string input)   //檢查字串是否為空值
    {
        return string.IsNullOrEmpty(input) ? "" : input;
    }
    IEnumerator UploadImageCoroutine(string imageName)  //上傳圖片到資料庫
    {
        StartCoroutine(LoadingPanel(true, 0));

        // 目標路徑
        string path = authorPath + "/書庫/" + storyBookNumber + "/書頁/" + databaseSort+ "/360互動內容/";
        DatabaseReference br = FirebaseDatabase.DefaultInstance.GetReference(path);

        // 將 Texture2D 轉成 PNG byte array
        byte[] imageBytes = imageTexture2D.EncodeToPNG();

        // 建立 Firebase Storage 的參考// 建立 Firebase Storage 的參考
        var imageRef = firebaseStorageURL.Child(path).Child(databaseOrder.ToString()).Child(imageName + ".png");

        // 上傳圖片
        var uploadTask = imageRef.PutBytesAsync(imageBytes);
        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.Exception != null)
        {
            Debug.LogError("上傳失敗: " + uploadTask.Exception);
            yield break;
        }

        // 取得下載連結
        var getUrlTask = imageRef.GetDownloadUrlAsync();
        yield return new WaitUntil(() => getUrlTask.IsCompleted);

        if (getUrlTask.Exception != null)
        {
            Debug.LogError("取得下載網址失敗: " + getUrlTask.Exception);
            yield break;
        }

        string downloadUrl = getUrlTask.Result.ToString();
       
        br.Child(databaseOrder.ToString()).Child("內容").SetValueAsync(downloadUrl);

        StartCoroutine(LoadingPanel(false, 0));
    }

    // 開啟檔案選單
    public void OpenFileMenu()
    {
        isClicked = true;         // 檢測是否點擊
    }

    // GUI事件處理
    private void OnGUI()
    {
        if (isClicked)
        {
            HandleFileSelection();             // 執行圖片匯入
            isClicked = false;
        }
    }
    // 處理檔案選擇
    private void HandleFileSelection()
    {
        OpenFileName openFileDialog = new OpenFileName();
        openFileDialog.structSize = Marshal.SizeOf(openFileDialog);
        openFileDialog.filter = "All Files\0*.*\0\0";
        openFileDialog.file = new string(new char[256]);
        openFileDialog.maxFile = openFileDialog.file.Length;
        openFileDialog.fileTitle = new string(new char[64]);
        openFileDialog.maxFileTitle = openFileDialog.fileTitle.Length;
        openFileDialog.initialDir = Application.dataPath;
        openFileDialog.title = " ";
        openFileDialog.defExt = "XML";
        openFileDialog.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
        if (WindowDll.GetOpenFileName(openFileDialog))
        {
            StartCoroutine(LoadingProgress(true, 0));
            StartCoroutine(UpdateMediaContent(openFileDialog.file, selectedImage));
        }     
    }

    private IEnumerator UpdateMediaContent(string fileUrl, Image targetImage)
    {
        yield return null;        // 等待一幀，確保函式異步啟動
        processedUrl = "";       // 初始化處理過的 URL 字串

        // 處理新媒體匯入
        processedUrl = fileUrl;
        StartCoroutine(LoadingProgress(false, 1.5f));            // 啟動載入進度顯示協程

        if (processedUrl != "" )
        {
            UnityWebRequest wr = UnityWebRequest.Get(fileUrl);  // 建立 UnityWebRequest 用來下載圖片檔案
            yield return wr.SendWebRequest();                   // 等待下載完成
            if (wr.isDone)                                     // 確認下載完成
            {
                byte[] r = wr.downloadHandler.data;           // 取得下載的原始二進位資料
                Texture2D t2D = new Texture2D(0, 0, TextureFormat.RGBA32, false);     // 建立一個空的 Texture2D，格式為 RGBA32
                t2D.LoadImage(r);                                                     // 將下載的圖片資料載入到 Texture2D 物件
                imageTexture2D = t2D;                                                 // 存起來方便後續使用
                image = Sprite.Create(t2D, new Rect(0, 0, t2D.width, t2D.height), Vector2.zero);    // 根據載入的 Texture2D 建立一個 Sprite
                Imagedisplay(targetImage, true);                                              // 顯示圖片
                targetImage.sprite = image;                                            // 把新建立的 Sprite 指派給目標 UI Image
                float a = (float)t2D.width / (float)t2D.height;
                targetImage.GetComponent<AspectRatioFitter>().aspectRatio = a;    // 計算圖片寬高比，並設定給 AspectRatioFitter 元件，保持圖片比例
            }
            yield break;
        }
    }


    // 顯示載入面板
    public IEnumerator LoadingPanel(bool show, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (show)
            loadingPanel.anchoredPosition = Vector2.zero;
        else
            loadingPanel.gameObject.GetComponent<UIStartPosition>()._CI();
    }
    // 顯示編輯載入面板
    public IEnumerator LoadingProgress(bool show, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (show)
            loadingPanelInProgress.anchoredPosition = Vector2.zero;
        else
            loadingPanelInProgress.gameObject.GetComponent<UIStartPosition>()._CI();
    }
   
     // 設置圖片顯示狀態
    private void Imagedisplay(Image targetImage, bool isVisible)
    {
        // 若 id 為 true，將圖片的顏色設為不透明（完全顯示）
        if (isVisible)
            targetImage.color = new Color(1, 1, 1, 1);                    // 設定圖片顏色為白色且不透明
        else
            targetImage.color = new Color(1, 1, 1, 0);                    // 設定圖片顏色為白色且透明（隱藏）
    }

}
