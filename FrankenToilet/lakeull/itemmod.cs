using BepInEx;
using BepInEx.Logging;
using FrankenToilet;
using FrankenToilet.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/*
 *   --- Public Apology ---
 * Dear all future coders of this project.
 * I deeply regret to inform you that the code in this project is fucking dogshit
 * Please find it in your heart to forgive me.
*/

namespace itemMod
{
    

    [EntryPoint]
    public class ItemModMain
    {
        private static GameObject[] packedObjects = [];
        private static AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "bundled")); // change name of "bundled" to the file name of the bundle
        private static string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "bundled");
        public static GameObject itemCanvas;
        public static GameObject itemBox;
        public static GameObject refillSfx;
        public static GameObject useSfx;
        private static GameObject explosionIcon;
        private static GameObject coinIcon;
        private static GameObject iglooIcon;
        public static bool canUseItem = false;
        private static List<GameObject> abilityIcons = new List<GameObject>();
        private static int abilityIndex;
        public static GameObject iglooObject;

        /*
         * Todo:
         * - update
         * - add scene filtering to bundle loading
         */


        [EntryPoint]
        public static void Awake()
        {
            // scene
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(OnSceneLoaded);
            // Plugin startup logic
            LogHelper.LogInfo($"Lakeull's Plugin is loaded!");
            LogHelper.LogInfo(bundlePath);
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            LogHelper.LogInfo("loading bundle (itemMod) " + bundlePath);
            LoadBundle();
        }

        public static void LoadBundle()
        {
            canUseItem = false;
            // load bundle, find the item canvas
            packedObjects = bundle.LoadAllAssets<GameObject>();
            foreach (GameObject gameObject in packedObjects)
            {
                // grabs the name of the specific item I WANT IT!!!!!!!!!!!
                //LogHelper.LogInfo($"{gameObject.name}");
                if ($"{gameObject.name}" == "Item Canvas")
                {
                    itemCanvas = GameObject.Instantiate(gameObject, new Vector3(0, 0, 0), Quaternion.identity);
                }
                if ($"{gameObject.name}" == "igloo")
                {
                    iglooObject = gameObject;
                }
                else
                {
                    LogHelper.LogError("bundle error: item canvas name was not identified.");
                }
                LogHelper.LogInfo($"{gameObject.name}");
            }
            // gets the item box sprite
            itemBox = GameObject.Find(itemCanvas.name + "/Item Box");
            refillSfx = GameObject.Find(itemCanvas.name + "/refill sfx");
            useSfx = GameObject.Find(itemCanvas.name + "/use sfx");

            // determine whether to reposition the item box 
            if (PrefsManager.Instance.GetInt("weaponHoldPosition") == 2)
            {
                LogHelper.LogInfo("2");
                itemBox.transform.localPosition = new Vector3(-800, -380);
            }
            else
            {
                itemBox.transform.localPosition = new Vector3(800, -380);
            }
            itemBox.AddComponent<ItemModUpdates>();


            InitialAssignPower();
            RandomizePower(); 
        }

        public static void InitialAssignPower()
        {
            // clear the list to not fuck anything up (it took me 3 hours to realize this)
            abilityIcons.Clear();

            // first ability: index 0
            explosionIcon = GameObject.Find(itemCanvas.name + "/" + itemBox.name + "/placeholder 1");
            explosionIcon.SetActive(false);
            abilityIcons.Add(explosionIcon);

            // second ability: index 1
            coinIcon = GameObject.Find(itemCanvas.name + "/" + itemBox.name + "/placeholder 2");
            coinIcon.SetActive(false);
            abilityIcons.Add(coinIcon);

            // third ability: index 2
            iglooIcon = GameObject.Find(itemCanvas.name + "/" + itemBox.name + "/iglooicon");
            iglooIcon.SetActive(false);
            abilityIcons.Add(iglooIcon);

            foreach (GameObject ability in abilityIcons)
            {
                ability.AddComponent<HudOpenEffect>();
            }
        }
        
        public static void RandomizePower()
        {
            // define random
            int randomGen = UnityEngine.Random.Range(0, abilityIcons.Count);
            abilityIndex = randomGen;
            abilityIcons[randomGen].SetActive(true);

            // reset canUseItem
            canUseItem = true;
        }

        public static void disableAllIcons()
        {
            foreach (GameObject item in abilityIcons)
            {
                item.SetActive(false);
            }
        }
        public static void usePower()
        {
            // ability 0, kill yourself (self destruct)
            if (abilityIndex == 0)
            {
                // loads explosion, makes it big
                GameObject exploder = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Malicious Railcannon.prefab").WaitForCompletion();
                exploder.transform.SetPositionAndRotation(NewMovement.Instance.transform.position, new Quaternion(0f, 0f, 0f, 0f));
                exploder.transform.Find("Sphere_8").GetComponent<Explosion>().maxSize = 100;
                exploder.transform.Find("Sphere_8").GetComponent<Explosion>().speed = 15;
                exploder.transform.localScale = new Vector3(3f, 3f, 3f);
                GameObject.Instantiate(exploder);
            }
            // ability 1, big fucking coin
            else if (abilityIndex == 1)
            {
                // loads coin, makes it big, makes it do crazy fucking wicked damage
                GameObject bigcoinaddress = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Coin.prefab").WaitForCompletion();

                GameObject bigcoin = GameObject.Instantiate(bigcoinaddress);
                bigcoin.transform.SetPositionAndRotation(NewMovement.Instance.transform.position, new Quaternion(0f, 0f, 0f, 0f));
                bigcoin.transform.GetComponent<Rigidbody>().useGravity = false;
                bigcoin.transform.GetComponent<Rigidbody>().velocity = new Vector3(0f, 5f, 0f);
                bigcoin.transform.localScale = new Vector3(10f, 10f, 10f);
                bigcoin.transform.Translate(new Vector3(0f, 3f, 0f));
                bigcoin.AddComponent<AlwaysLookAtCamera>();
                bigcoin.GetComponent<Coin>().power = 30;
                bigcoin.GetComponent<Coin>().ricochets = 4;
            }
            // ability 2, igloo
            else if (abilityIndex == 2)
            {
                if(GameObject.Find("igloo") == null)
                { 
                    // make new igloo
                    GameObject iglooObjectInstance = GameObject.Instantiate(iglooObject);
                    iglooObjectInstance.transform.position = NewMovement.Instance.transform.position;
                } else
                {
                    // set position of existing igloo to player location if its alr present
                    GameObject.Find("igloo").transform.position = NewMovement.Instance.transform.position;
                }
            }

            // plays the use sound effect
            useSfx.GetComponent<AudioSource>().Play();
        }
    }

    [EntryPoint]
    public class ItemModUpdates : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z) && ItemModMain.canUseItem == true)
            {
                LogHelper.LogInfo("using power.");
                ItemModMain.usePower();
                ItemModMain.disableAllIcons();
                StartCoroutine(Cooldown());// counts for 30 seconds
            }
        }
        public static IEnumerator Cooldown()
        {
            ItemModMain.canUseItem = false;
            yield return new WaitForSeconds(5);
            ItemModMain.RandomizePower();
            // play the refill sound effect
            ItemModMain.refillSfx.GetComponent<AudioSource>().Play();
        }
    }
}