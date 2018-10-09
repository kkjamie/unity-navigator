using System;
using UnityEngine;

namespace UnityNavigator
{
	public static class Transitions
	{
		public static void Instant(GameObject from, GameObject to, Action onComplete = null)
		{
			if (from != null) from.SetActive(false);
			if (to != null) to.SetActive(true);
			if (onComplete != null) onComplete();
		}
	}
}