using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GestureListenerTeach : MonoBehaviour, KinectGestures.GestureListenerInterface
{
    [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

    private bool progressDisplayed;
	private float progressGestureTime;

	public Teachmove player;
	public Manual manual;

	void Start()
    {
        player=FindObjectOfType<Teachmove>();
		manual = FindObjectOfType<Manual>();
	}


    public void UserDetected(long userId, int userIndex)
	{
		if (userIndex != playerIndex)
			return;

		// as an example - detect these user specific gestures
		KinectManager manager = KinectManager.Instance;
		manager.DetectGesture(userId, KinectGestures.Gestures.LeanLeft);
		manager.DetectGesture(userId, KinectGestures.Gestures.LeanRight);
		manager.DetectGesture(userId, KinectGestures.Gestures.Jump);
		manager.DetectGesture(userId, KinectGestures.Gestures.SwipeRight);
	}

    public void UserLost(long userId, int userIndex)
	{
		if (userIndex != playerIndex)
			return;
	}

    public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return;

		if(gesture == KinectGestures.Gestures.LeanRight && progress > 0.3f)
		{

			//向右傾斜
			player.hide(1);
			progressDisplayed = true;
			progressGestureTime = Time.realtimeSinceStartup;

		}if(gesture == KinectGestures.Gestures.LeanLeft && progress > 0.3f)
		{
			//向左傾斜
			player.hide(-1);
			progressDisplayed = true;
			progressGestureTime = Time.realtimeSinceStartup;

		}
	}

    public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return false;

		if (gesture == KinectGestures.Gestures.Jump)
		{
			player.jumpcan();
		}
		if (progressDisplayed)
			return true;
		if (gesture == KinectGestures.Gestures.SwipeRight)
		{
			manual.pause();
		}

		return true;
	}

	public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		if (userIndex != playerIndex)
			return false;

		if(progressDisplayed)
		{
			progressDisplayed = false;
			player.hide(0);

		}
		
		return true;
	}
	
	public void Update()
	{
		if(progressDisplayed && ((Time.realtimeSinceStartup - progressGestureTime) > 2f))
		{
			progressDisplayed = false;
			
			Debug.Log("Forced progress to end.");
		}	
	}

}
