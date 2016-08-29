using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PoolData
{
	public string prefab = ""; // prefab reference
	public GameObject holder; // holder object for these pool objects
	public List<GameObject> freeObjects; // list of free objects
	public List<GameObject> inUseObjects; // list of objects in use;

	// allows the creation of a new object when the pool is empty
	// PIETER: I prefer 'forceReturning' an object, so that is true per default.
	// However, there might be certain cases where you NEED a new object when the pool is empty. Maybe with homing missiles that live until they hit something,.. I dunno
	public bool allowNew = false;

	// takes the 'oldest' in use object and return it to the pool, then use it.
	// NOTE: Keep in mind that when your pool has not got many objects you can experience sudden dissapearances of your objects.
	// For explosions this won't matter, but maybe for projectiles it can be problematic.
	public bool forceReturn = true;

	private GameObject source; // source reference
	private GameObject lastClone; // last clone

	// this is a list of components you DON'T want to delete. For example you DO want to delete a projectile script from a projectile.
	// but you DON'T WANT to delete a particle system which you have one setup
	// per default we ignore Transform, MeshFilter, MeshRenderer and ParticleSystem for obvious reasons ;) 
	public List<string> ignoreComponents = new List<string>(){"Transform", "MeshFilter", "MeshRenderer", "ParticleSystem"};

	public void Initialize(string aPath, string aPrefab, int anAmount){
		prefab = aPrefab; // store prefab name
		holder = new GameObject(aPrefab+"_Pool"); // new gameobject
		holder.transform.ResetToParent(PoolManager.poolHolder); // parent this holder to the PoolManager holder
		freeObjects = new List<GameObject>(); // new list
		inUseObjects = new List<GameObject>(); //new list
		// create/load source
		source = Loader.LoadGameObject(aPath + aPrefab + "_Prefab");
		source.transform.ResetToParent(holder);
		freeObjects.Add(source);
		for (int i = 0; i < anAmount-1; i++) { // create the amount of objects
			CreateObject(source);
		}
		holder.SetActive(false); // Hide the holder so we dont have all those objects floating in the game
	}

	// Instantiates the source
	void CreateObject(GameObject aSourceObject){
		lastClone = GameObject.Instantiate(aSourceObject);
		lastClone.name = source.name;
		lastClone.transform.ResetToParent(holder);
		freeObjects.Add(lastClone);
	}

	/// <summary>
	/// Gets an object.
	/// </summary>
	/// <returns>The object.</returns>
	public GameObject GetObject(){
		// what should we do
		GameObject tObject = null;
		if (freeObjects.Count == 0){ // There are no free objects
			if(allowNew){ // if we allow new ones; create it
				CreateObject(source);
				tObject = GetObject();
			} else if (forceReturn){ // force return the oldest!
				ReturnObject(inUseObjects[0]);
				tObject = GetObject();
			}
		} else { // there are free ones, return the first
			tObject = freeObjects[0];
			freeObjects.RemoveAt(0);
		}
		// if there was an object, set some stuff
		if (tObject != null){
			tObject.transform.parent = null; // THIS IS MOST IMPORTANT, IF we don't do this SetActive below has no effect!
			tObject.SetActive(true); // activate
			inUseObjects.Add(tObject); // add to inuse list
		} 
		return tObject; // return it
	}

	/// <summary>
	/// Returns an object.
	/// </summary>
	/// <param name="anObject">An object.</param>
	public void ReturnObject(GameObject anObject){
		// remove from list
		inUseObjects.Remove(anObject);
		// clear component from 
		Component[] components = anObject.GetComponents(typeof(Component));
		foreach(Component component in components){
			string name = component.GetType().Name;
			if (!ignoreComponents.Contains(name)) Object.Destroy(component);
		}
		// set back
		anObject.transform.ResetToParent(holder);
		anObject.SetActive(false);
		// add to liust
		freeObjects.Add(anObject);
	}


	public void Destroy(){
		// return all objects to the pool!
		for (int i = inUseObjects.Count-1; i >= 0; i--) {
			ReturnObject(inUseObjects[i]);
		}
		// delete holder
		Object.Destroy(holder);
	}
}

