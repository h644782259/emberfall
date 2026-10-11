using System;using System.Collections.Generic;using UnityEngine;
namespace Emberfall
{
    // Transform callbacks invalidate membership without scanning on every visual tick.
    // Adding/replacing only a Renderer component on an existing GameObject has no Unity
    // hierarchy callback: that caller must invoke Invalidate explicitly.
    internal sealed class RendererGroupCache : IDisposable
    {
        private Transform root;
        private Renderer[] renderers=new Renderer[0];
        private readonly List<RendererGroupObserver> observers=new List<RendererGroupObserver>();
        private bool dirty=true;
        internal int Revision {get;private set;}
        internal bool Dirty {get{return dirty;}}
        internal RendererGroupCache(Transform owner){root=owner;}
        internal void Invalidate()
        {
            dirty=true;
            // A removed subtree may outlive this model and may never trigger Read again.
            // Release its observer subscriptions at the structural event, not lazily.
            for(int i=observers.Count-1;i>=0;i--)
            {
                var observer=observers[i];
                if(observer!=null&&root!=null&&(observer.transform==root||observer.transform.IsChildOf(root)))continue;
                if(observer!=null)observer.Remove(this);
                observers.RemoveAt(i);
            }
        }
        internal static void Invalidate(Transform part)
        {
            if(part==null)return;
            var observer=part.GetComponent<RendererGroupObserver>();
            if(observer!=null)observer.InvalidateOwners();
        }
        internal Renderer[] Read()
        {
            if(!dirty)return renderers;
            foreach(var observer in observers)if(observer!=null)observer.Remove(this);
            observers.Clear();
            renderers=root==null?new Renderer[0]:root.gameObject.GetComponentsInChildren<Renderer>(true);
            if(root!=null)foreach(var part in root.gameObject.GetComponentsInChildren<Transform>(true))
            {
                var observer=part.GetComponent<RendererGroupObserver>();
                if(observer==null)observer=part.gameObject.AddComponent<RendererGroupObserver>();
                observer.Add(this);observers.Add(observer);
            }
            dirty=false;Revision++;return renderers;
        }
        public void Dispose()
        {
            foreach(var observer in observers)if(observer!=null)observer.Remove(this);
            observers.Clear();renderers=new Renderer[0];root=null;dirty=false;
        }
    }
    internal sealed class RendererGroupObserver : MonoBehaviour
    {
        private readonly List<RendererGroupCache> owners=new List<RendererGroupCache>();
        internal void Add(RendererGroupCache owner){if(!owners.Contains(owner))owners.Add(owner);}
        internal void Remove(RendererGroupCache owner){owners.Remove(owner);}
        internal void InvalidateOwners(){foreach(var owner in owners.ToArray())owner.Invalidate();}
        private void Changed(){InvalidateOwners();}
        private void OnTransformChildrenChanged(){Changed();}
        private void OnTransformParentChanged(){Changed();}
        private void OnDestroy(){Changed();owners.Clear();}
    }
}
