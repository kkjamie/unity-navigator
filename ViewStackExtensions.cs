using System;
using UnityEngine;

namespace UnityNavigator
{
	public static class ViewStackExtensions
	{
		public static void Navigate(
			this ViewStack viewStack,
			NavigationAction action,
			string newScreenId,
			Action<GameObject> initView,
			ViewStack.ViewTransition transition = null)
		{
			// this would contain a switch to call the appropriate function
			switch (action)
			{
				case NavigationAction.Push:
					viewStack.Push(newScreenId, initView, transition);
					break;
				case NavigationAction.Pop:
					viewStack.Pop(transition);
					break;
			}
		}

		public static void Navigate<TArgs>(
			this ViewStack viewStack,
			NavigationAction action,
			string newScreenId,
			TArgs args,
			ViewStack.ViewTransition transition = null)
		{
			viewStack.Navigate<IViewBehaviour<TArgs>>(
				action,
				newScreenId,
				v => v.Init(args),
				transition
			);
		}

		public static void Navigate<TViewBehaviour>(
			this ViewStack viewStack,
			NavigationAction action,
			string viewId,
			Action<TViewBehaviour> initView = null,
			ViewStack.ViewTransition transition = null)
		{
			viewStack.Navigate(action, viewId, view =>
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

		public static void Navigate<TViewBehaviour, TArgs>(
			this ViewStack viewStack,
			NavigationAction action,
			string viewId,
			TArgs args,
			ViewStack.ViewTransition transition = null) where TViewBehaviour : IViewBehaviour<TArgs>
		{
			viewStack.Navigate<TViewBehaviour>(
				action,
				viewId,
				v => v.Init(args),
				transition
			);
		}
	}
}