using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityNavigator
{
	public class ViewStack : MonoBehaviour
	{
		public delegate void ViewTransition(GameObject from, GameObject to, Action onComplete = null);

		public interface ITransitionStartedHandler { void HandleTransitionStarted(); }
		public interface ITransitionCompleteHandler { void HandleTransitionComplete(); }

		private class ViewStackEntry
		{
			public string ViewID { get; private set; }
			public GameObject View { get; private set; }

			public ViewStackEntry(string id, GameObject view)
			{
				ViewID = id;
				View = view;
			}

			public void NotifyView<T>(Action<T> notify, bool includeChildren = false)
			{
				var components = includeChildren
					? View.GetComponents<T>() : View.GetComponentsInChildren<T>();
				foreach (var component in components)
				{
					notify(component);
				}
			}
		}

		public ViewTransition DefaultTransition { get; set; }

		private ViewStackEntry TopViewEntry
		{
			get { return viewStack.Count > 0 ? viewStack[viewStack.Count - 1] : null; }
		}

		private List<ViewStackEntry> viewStack;
		private IViewCreator viewCreator;
		private bool transitionIsInProgress;

		public event Action<string, GameObject> OnViewCreated;
		public event Action<string, GameObject> OnViewDestroyed;
		public event Action<string, GameObject> OnViewShown;
		public event Action<string, GameObject> OnViewHidden;

		private void Awake()
		{
			viewStack = new List<ViewStackEntry>();
		}

		private void Start()
		{
			viewCreator = GetComponent<IViewCreator>();
			if (viewCreator == null)
			{
				throw new Exception("ViewStack doens't have an IViewCreator attached to it.");
			}
		}

		/// <summary>
		/// Experimental api - could be extension methods
		/// </summary>
		public void Navigate(
			string newScreenId,
			NavigationAction action,
			Action<GameObject> initView,
			ViewTransition transition = null)
		{
			// this would contain a switch to call the appropriate function
			switch (action)
			{
				case NavigationAction.Push:
					Push(newScreenId, initView, transition);
					break;
				case NavigationAction.Pop:
					Pop(transition);
					break;
			}

			// an overload could templetize it TViewBehaviour and take out the gameObject from init,
			// and find the actual script you want

			// further to that one could pass data in and look for an interface
			// IViewBehaviour<TViewData> with an Initialize(TViewData data)
		}

		public void Push(string newScreenId, Action<GameObject> initView = null, ViewTransition transition = null)
		{
			if (transitionIsInProgress)
			{
				// TODO: How to handle this?
				// for now just return and prevent starting a new one whilst one is in progress
				return;
			}

			var newView = CreateView(newScreenId);
			if (initView != null) initView(newView);

			PerformTransition(
				transition,
				() => viewStack.Add(new ViewStackEntry(newScreenId, newView))
			);
		}

		public void Pop(ViewTransition transition = null)
		{
			if (transitionIsInProgress)
			{
				// TODO: How to handle this?
				// for now just return and prevent starting a new one whilst one is in progress
				return;
			}

			var oldTopViewEntry = TopViewEntry;

			PerformTransition(
				transition,
				() => viewStack.Remove(oldTopViewEntry),
				() => DestroyView(oldTopViewEntry)
			);
		}

		// TODO: add other operations like replace, pop-replace, reset etc...

		private void PerformTransition(ViewTransition transition, Action manipulateState, Action onComplete = null)
		{
			transition = transition ?? DefaultTransition ?? Transitions.Instant;

			var oldTopViewEntry = TopViewEntry;
			manipulateState();

			if (oldTopViewEntry != null)
			{
				// notify old view that transition has started
				oldTopViewEntry.NotifyView<ITransitionStartedHandler>(
					t => t.HandleTransitionStarted());
			}

			var oldView = oldTopViewEntry != null ? oldTopViewEntry.View : null;
			transitionIsInProgress = true;
			transition(oldView, TopViewEntry.View, () =>
			{
				// notify the new view that transition has completed
				TopViewEntry.NotifyView<ITransitionCompleteHandler>(
					t => t.HandleTransitionComplete());

				if (OnViewHidden != null) OnViewHidden(oldTopViewEntry.ViewID, oldTopViewEntry.View);
				if (OnViewShown != null) OnViewShown(TopViewEntry.ViewID, TopViewEntry.View);

				transitionIsInProgress = false;

				if (onComplete != null)
				{
					onComplete();
				}
			});
		}

		private GameObject CreateView(string newScreenId)
		{
			var view = viewCreator.Create(newScreenId);
			if (OnViewCreated != null) OnViewCreated(newScreenId, view);
			return view;
		}

		private void DestroyView(ViewStackEntry oldTopViewEntry)
		{
			if (OnViewDestroyed != null) OnViewDestroyed(oldTopViewEntry.ViewID, oldTopViewEntry.View);
			viewCreator.Destroy(oldTopViewEntry.ViewID, oldTopViewEntry.View);
		}
	}
}