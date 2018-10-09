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
		public string TopViewID => TopViewEntry?.ViewID;
		private ViewStackEntry TopViewEntry => viewStack.Count > 0 ? viewStack[viewStack.Count - 1] : null;

		private List<ViewStackEntry> viewStack;
		private IViewCreator viewCreator;
		private bool transitionIsInProgress;
		private bool isInitialized;

		public event Action<string, GameObject> OnViewCreated;
		public event Action<string, GameObject> OnViewDestroyed;
		public event Action<string, GameObject> OnViewShown;
		public event Action<string, GameObject> OnViewHidden;
		// From ID -> To ID
		public event Action<string, string> OnTransitionStarted;
		public event Action<string, string> OnTransitionComplete;

		private void Awake()
		{
			viewStack = new List<ViewStackEntry>();
		}

		private void Start()
		{
			if (!isInitialized)
			{
				Init();
			}
		}

		public void Init()
		{
			viewCreator = GetComponent<IViewCreator>();
			if (viewCreator == null)
			{
				throw new Exception("ViewStack doens't have an IViewCreator attached to it.");
			}

			isInitialized = true;
		}

		public void Push(string newScreenId, Action<GameObject> initView = null, ViewTransition transition = null)
		{
			if (!isInitialized) throw new Exception("ViewStack is not yet initialized.");

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
			if (!isInitialized) throw new Exception("ViewStack is not yet initialized. Listen for OnInitialized before you start navigating");

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
			transition(oldView, TopViewEntry != null ? TopViewEntry.View : null, () =>
			{
				// notify the new view that transition has completed
				if (TopViewEntry != null)
				{
					TopViewEntry.NotifyView<ITransitionCompleteHandler>(
						t => t.HandleTransitionComplete());
				}

				if (OnTransitionComplete != null)
				{
					OnTransitionComplete(oldTopViewEntry == null ? null : oldTopViewEntry.ViewID, TopViewEntry.ViewID);
				}

				if (OnViewHidden != null && oldTopViewEntry != null)
				{
					OnViewHidden(oldTopViewEntry.ViewID, oldTopViewEntry.View);
				}

				if (OnViewShown != null)
				{
					OnViewShown(TopViewEntry.ViewID, TopViewEntry.View);
				}

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