using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityNavigator
{
	public class PrefabViewCreator : MonoBehaviour, IViewCreator
	{
		[Serializable]
		private class ViewDefinition
		{
			public string viewID;
			public string resourcePath;
		}

		[SerializeField]
		private List<ViewDefinition> viewsList;

		private Dictionary<string, string> views;

		private void Awake()
		{
			views = viewsList.ToDictionary(v => v.viewID, v => v.resourcePath);
		}

		public GameObject Create(string id)
		{
			if (!views.ContainsKey(id))
			{
				throw new Exception("Cannot find view configured with the id: " + id);
			}

			var viewPrefab = Resources.Load<GameObject>(views[id]);
			if (viewPrefab == null)
			{
				throw new Exception("No view found at " + views[id]);
			}

			var view = Instantiate(viewPrefab);
			view.name = viewPrefab.name;
			view.transform.SetParent(transform, false);
			return view;
		}

		public void Destroy(string id, GameObject view)
		{
			Destroy(view);
		}
	}
}