using UnityEngine;

namespace UnityNavigator
{
	public interface IViewCreator
	{
		GameObject Create(string id);
		void Destroy(string id, GameObject view);
	}
}