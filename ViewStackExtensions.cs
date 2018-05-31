using System;

namespace UnityNavigator
{
	public static class ViewStackExtensions
	{
		public static void Navigate<TViewBehaviour>(
			this ViewStack viewStack,
			string viewId,
			NavigationAction action,
			Action<TViewBehaviour> initView = null,
			ViewStack.ViewTransition transition = null)
		{
			viewStack.Navigate(viewId, action, view =>
			{
				var viewBehaviour = view.GetComponent<TViewBehaviour>();
				if (viewBehaviour == null)
				{
					throw new Exception("View behaviour is missing from view: " + typeof(TViewBehaviour));
				}

				if (initView != null)
				{
					initView(viewBehaviour);
				}
			}, transition);
		}
	}
}