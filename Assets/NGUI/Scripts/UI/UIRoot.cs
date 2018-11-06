//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2016 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This is a script used to keep the game object scaled to 2/(Screen.height).
/// If you use it, be sure to NOT use UIOrthoCamera at the same time.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Root")]
public class UIRoot : MonoBehaviour
{
	/// <summary>
	/// List of all UIRoots present in the scene.
	/// </summary>

	static public List<UIRoot> list = new List<UIRoot>();

	public enum Scaling
	{
		Flexible,
		Constrained,
		ConstrainedOnMobiles,
	}

	public enum Constraint
	{
		Fit,
		Fill,
		FitWidth,
		FitHeight,
	}

    /// <summary>
    /// Type of scaling used by the UIRoot.
    /// UIRoot使用的缩放类型。
    /// </summary>

    public Scaling scalingStyle = Scaling.Flexible;

    /// <summary>
    /// When the UI scaling is constrained, this controls the type of constraint that further fine-tunes how it's scaled.
    /// 当UI缩放受到约束时，它会控制约束类型，进一步微调它的缩放方式。
    /// </summary>

    public Constraint constraint
	{
		get
		{
			if (fitWidth)
			{
				if (fitHeight) return Constraint.Fit;
				return Constraint.FitWidth;
			}
			else if (fitHeight) return Constraint.FitHeight;
			return Constraint.Fill;
		}
	}

    /// <summary>
    /// Width of the screen, used when the scaling style is set to Flexible.
    /// 屏幕宽度，在缩放样式设置为“灵活”时使用。
    /// </summary>

    public int manualWidth = 1280;

    /// <summary>
    /// Height of the screen when the scaling style is set to FixedSize or Flexible.
    ///屏幕高度, 缩放样式设置为FixedSize或Flexible时使用。
    /// </summary>

    public int manualHeight = 720;

    /// <summary>
    /// If the screen height goes below this value, it will be as if the scaling style
    /// is set to FixedSize with manualHeight of this value.
    /// 如果屏幕高度低于此值（最小值），则会像缩放样式一样
    /// 设置为FixedSize，manualHeight等于minimumHeight。
    /// </summary>

    public int minimumHeight = 320;

    /// <summary>
    /// If the screen height goes above this value, it will be as if the scaling style
    /// is set to Fixed Height with manualHeight of this value.
    /// 如果屏幕高度超过此值（最大值），则会像缩放样式一样
    /// 设置为Fixed Height，manualHeight等于maximumHeight。
    /// </summary>

    public int maximumHeight = 1536;

    /// <summary>
    /// When Constraint is on, controls whether the content must be restricted horizontally to be at least 'manualWidth' wide.
    /// 启用“约束”时，控制是否必须将【内容水平限制】为至少为“manualWidth”宽度。
    /// </summary>

    public bool fitWidth = false;

    /// <summary>
    /// When Constraint is on, controls whether the content must be restricted vertically to be at least 'Manual Height' tall.
    /// 启用“约束”时，控制是否必须【垂直限制内容】至少“手动高度”高。
    /// </summary>

    public bool fitHeight = true;

    /// <summary>
    /// Whether the final value will be adjusted by the device's DPI setting.
    /// Used when the Scaling is set to Pixel-Perfect.
    /// 是否将通过设备的DPI设置调整最终值。
    /// 当Scaling设置为Pixel-Perfect时使用。
    /// </summary>

    public bool adjustByDPI = false;

    /// <summary>
    /// If set and the game is in portrait mode, the UI will shrink based on the screen's width instead of height.
    /// Used when the Scaling is set to Pixel-Perfect.
    /// 如果设置并且游戏处于纵向模式，则UI将根据屏幕的宽度而不是高度缩小。
    /// 当Scaling设置为Pixel-Perfect时使用。
    /// </summary>

    public bool shrinkPortraitUI = false;

    /// <summary>
    /// Active scaling type, based on platform.
    /// 基于平台的活动缩放类型。
    /// </summary>

    public Scaling activeScaling
	{
		get
		{
			Scaling scaling = scalingStyle;

			if (scaling == Scaling.ConstrainedOnMobiles)
#if UNITY_EDITOR || UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_WP_8_1 || UNITY_BLACKBERRY
				return Scaling.Constrained;
#else
				return Scaling.Flexible;
#endif
			return scaling;
		}
	}

    /// <summary>
    /// UI Root's active height, based on the size of the screen.
    /// UI Root的活动高度，基于屏幕大小。
    /// </summary>

    public int activeHeight
	{
		get
		{
			Scaling scaling = activeScaling;

			if (scaling == Scaling.Flexible)
			{
				Vector2 screen = NGUITools.screenSize;
				float aspect = screen.x / screen.y;

				if (screen.y < minimumHeight)
				{
					screen.y = minimumHeight;
					screen.x = screen.y * aspect;
				}
				else if (screen.y > maximumHeight)
				{
					screen.y = maximumHeight;
					screen.x = screen.y * aspect;
				}

				// Portrait mode uses the maximum of width or height to shrink the UI
				int height = Mathf.RoundToInt((shrinkPortraitUI && screen.y > screen.x) ? screen.y / aspect : screen.y);

				// Adjust the final value by the DPI setting
				return adjustByDPI ? NGUIMath.AdjustByDPI(height) : height;
			}
			else
			{
				Constraint cons = constraint;
				if (cons == Constraint.FitHeight)
					return manualHeight;

				Vector2 screen = NGUITools.screenSize;
				float aspect = screen.x / screen.y;
				float initialAspect = (float)manualWidth / manualHeight;

				switch (cons)
				{
					case Constraint.FitWidth:
					{
						return Mathf.RoundToInt(manualWidth / aspect);
					}
					case Constraint.Fit:
					{
						return (initialAspect > aspect) ?
							Mathf.RoundToInt(manualWidth / aspect) :
							manualHeight;
					}
					case Constraint.Fill:
					{
						return (initialAspect < aspect) ?
							Mathf.RoundToInt(manualWidth / aspect) :
							manualHeight;
					}
				}
				return manualHeight;
			}
		}
	}

    /// <summary>
    /// Pixel size adjustment. Most of the time it's at 1, unless the scaling style is set to FixedSize.
    /// 像素大小调整。 除非缩放样式设置为FixedSize，否则大多数情况下它处于1。
    /// </summary>

    public float pixelSizeAdjustment
	{
		get
		{
			int height = Mathf.RoundToInt(NGUITools.screenSize.y);
			return height == -1 ? 1f : GetPixelSizeAdjustment(height);
		}
	}

    /// <summary>
    /// Helper function that figures out the pixel size adjustment for the specified game object.
    /// 辅助函数，用于指出指定游戏对象的像素大小调整。
    /// </summary>

    static public float GetPixelSizeAdjustment (GameObject go)
	{
		UIRoot root = NGUITools.FindInParents<UIRoot>(go);
		return (root != null) ? root.pixelSizeAdjustment : 1f;
	}

    /// <summary>
    /// Calculate the pixel size adjustment at the specified screen height value.
    /// 计算指定屏幕高度值的像素大小调整。
    /// </summary>

    public float GetPixelSizeAdjustment (int height)
	{
		height = Mathf.Max(2, height);

		if (activeScaling == Scaling.Constrained)
			return (float)activeHeight / height;

		if (height < minimumHeight) return (float)minimumHeight / height;
		if (height > maximumHeight) return (float)maximumHeight / height;
		return 1f;
	}

	Transform mTrans;

	protected virtual void Awake () { mTrans = transform; }
	protected virtual void OnEnable () { list.Add(this); }
	protected virtual void OnDisable () { list.Remove(this); }

	protected virtual void Start ()
	{
		UIOrthoCamera oc = GetComponentInChildren<UIOrthoCamera>();

		if (oc != null)
		{
			Debug.LogWarning("UIRoot should not be active at the same time as UIOrthoCamera. Disabling UIOrthoCamera.", oc);
			Camera cam = oc.gameObject.GetComponent<Camera>();
			oc.enabled = false;
			if (cam != null) cam.orthographicSize = 1f;
		}
		else UpdateScale(false);
	}

	void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying && gameObject.layer != 0)
			UnityEditor.EditorPrefs.SetInt("NGUI Layer", gameObject.layer);
#endif
		UpdateScale();
	}

    /// <summary>
    /// Immediately update the root's scale. Call this function after changing the min/max/manual height values.
    /// 立即更新根的比例。 更改最小/最大/手动高度值后调用此功能。
    /// </summary>

    public void UpdateScale (bool updateAnchors = true)
	{
		if (mTrans != null)
		{
			float calcActiveHeight = activeHeight;

			if (calcActiveHeight > 0f)
			{
				float size = 2f / calcActiveHeight;

				Vector3 ls = mTrans.localScale;

				if (!(Mathf.Abs(ls.x - size) <= float.Epsilon) ||
					!(Mathf.Abs(ls.y - size) <= float.Epsilon) ||
					!(Mathf.Abs(ls.z - size) <= float.Epsilon))
				{
					mTrans.localScale = new Vector3(size, size, size);
					if (updateAnchors) BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

    /// <summary>
    /// Broadcast the specified message to the entire UI.
    /// 将指定的消息广播到整个UI。
    /// </summary>

    static public void Broadcast (string funcName)
	{
#if UNITY_EDITOR
		if (Application.isPlaying)
#endif
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				UIRoot root = list[i];
				if (root != null) root.BroadcastMessage(funcName, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

    /// <summary>
    /// Broadcast the specified message to the entire UI.
    /// 将指定的消息广播到整个UI。
    /// </summary>

    static public void Broadcast (string funcName, object param)
	{
		if (param == null)
		{
			// More on this: http://answers.unity3d.com/questions/55194/suggested-workaround-for-sendmessage-bug.html
			Debug.LogError("SendMessage is bugged when you try to pass 'null' in the parameter field. It behaves as if no parameter was specified.");
		}
		else
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				UIRoot root = list[i];
				if (root != null) root.BroadcastMessage(funcName, param, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
}
