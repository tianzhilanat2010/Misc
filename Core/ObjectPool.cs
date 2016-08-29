// #define DISABLE_POOL
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GameEngine.Core
{
    public interface IPoolableObject<T> where T : class, IPoolableObject<T>
    {
        ObjectPool<T> Pool { get; set; }
        int IndexInPool { get; set; }
        void OnProducedByPool();
        void OnRecycledByPool();
    }

    public static class IPoolableObjectExtension
    {
        public static void Destroy<T>(this IPoolableObject<T> thiz) where T : class, IPoolableObject<T>
        {
            thiz.Pool.DestroyObject((T)thiz);
        }

        public static bool IsDestroyed<T>(this IPoolableObject<T> thiz) where T : class, IPoolableObject<T>
        {
            return thiz.Pool.IsDestroyed((T)thiz);
        }
    }

    public class ObjectPool<T> where T : class, IPoolableObject<T>
    {
        public ObjectPool(int size, Func<T> creator)
        {
            mCreator = creator;
#if !DISABLE_POOL
            mFirstEmptyObjectIndex = 0;
            mObjectList = new List<T>();
            Reserve(size);
#endif
        }

        public void Reserve(int size)
        {
#if !DISABLE_POOL
            if(size > mObjectList.Count)
            {
                for (int i = mObjectList.Count; i < size; i++)
                {
                    mObjectList.Add(mCreator());
                    mObjectList[i].Pool = this;
                    mObjectList[i].IndexInPool = i + 1;
                    mObjectList[i].OnRecycledByPool();
                }
                mLastEmptyObjectIndex = size - 1;
            }
#endif
        }

        public T CreateObject()
        {
#if !DISABLE_POOL
//            Debug.Assert(mFirstEmptyObjectIndex < mObjectList.Count);
            if(mFirstEmptyObjectIndex >= mObjectList.Count)
            {
                Reserve(Mathf.Max(mObjectList.Count * 2, 1));
            }
            T obj = mObjectList[mFirstEmptyObjectIndex];
            int swap = obj.IndexInPool;
            obj.IndexInPool = mFirstEmptyObjectIndex;
            mFirstEmptyObjectIndex = swap;
#else
            T obj = mCreator();
#endif
            obj.OnProducedByPool();
            return obj;
        }

        public bool IsDestroyed (T obj)
        {
            return (obj.IndexInPool >= mObjectList.Count) || (mObjectList[obj.IndexInPool] != obj);
        }

        public void DestroyObject(T obj)
        {
#if !DISABLE_POOL
            Debug.Assert(mObjectList[obj.IndexInPool] == obj);
            int swap = mObjectList[mLastEmptyObjectIndex].IndexInPool;
            mObjectList[mLastEmptyObjectIndex].IndexInPool = obj.IndexInPool;
            mLastEmptyObjectIndex = obj.IndexInPool;
            obj.IndexInPool = swap;
#endif
            obj.OnRecycledByPool();
        }

        private List<T> mObjectList;
        private int mFirstEmptyObjectIndex;
        private int mLastEmptyObjectIndex;
        private Func<T> mCreator;
    }
}
