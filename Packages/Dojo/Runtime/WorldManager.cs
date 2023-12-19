using System;
using System.Linq;
using bottlenoselabs.C2CS.Runtime;
using dojo_bindings;
using Dojo.Starknet;
// using UnityEditor.PackageManager;
using UnityEngine;
using Dojo.Torii;

namespace Dojo
{
    public class WorldManager : MonoBehaviour
    {
        [Header("RPC")] public string toriiUrl = "http://localhost:8080";
        public string rpcUrl = "http://localhost:5050";

        [Header("World")] public string worldAddress;
        public SynchronizationMaster synchronizationMaster;
        public ToriiClient toriiClient;
        private Account account;
        private JsonRpcClient provider;
        private readonly string playerAddress = "0x0517ececd29116499f4a1b64b094da79ba08dfd54a3edaa316134c41f8160973";
        private readonly string actionsAddress = "0x0152dcff993befafe5001975149d2c50bd9621da7cbaed74f68e7d5e54e65abc";

        private bool modelEntityUpdated = false;
        private bool entityUpdated = false;


        // public GameStateController gameStateController;

        // Start is called before the first frame update
        void Awake()
        {
            print("toriurl " + toriiUrl + " rpc url " + rpcUrl);
            print(new dojo.KeysClause[] { });
            print(dojo.signing_key_new().ToString());
            // create the torii client and start subscription service
            toriiClient = new ToriiClient(toriiUrl, rpcUrl, worldAddress, new dojo.KeysClause[] { });

            // gameStateController.client = toriiClient;
            // gameStateController.SetupAccount();
            // start subscription service
            toriiClient.StartSubscription();

            // fetch entities from the world
            // TODO: maybe do in the start function of the SynchronizationMaster?
            // problem is when to start the subscription service
            synchronizationMaster.SynchronizeEntities();

            // listen for entity updates
            synchronizationMaster.RegisterEntityCallbacks();
        }

        private void Start()
        {
            SetupAccount();
        }

        // Update is called once per frame
        void Update()
        {
        }


        public GameObject Entity(string name)
        {
            var entity = transform.Find(name);
            if (entity == null)
            {
                Debug.LogError($"Entity {name} not found");
                return null;
            }

            return entity.gameObject;
        }

        // return all children
        public GameObject[] Entities()
        {
            return transform.Cast<Transform>()
                .Select(t => t.gameObject)
                .ToArray();
        }

        public GameObject AddEntity(string key)
        {
            // check if entity already exists
            var entity = transform.Find(key)?.gameObject;
            if (entity != null)
            {
                Debug.LogWarning($"Entity {key} already exists");
                return entity.gameObject;
            }

            entity = new GameObject(key);
            entity.transform.parent = transform;

            return entity;
        }

        public void RemoveEntity(string key)
        {
            var entity = transform.Find(key);
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }


        public void SetupAccount()
        {
            provider = new JsonRpcClient(rpcUrl);

            var signer = new SigningKey("0x1800000000300000180000000000030000000000003006001800006600");

            account = new Account(provider, signer, playerAddress);
        }

        public void TestAccountAddress()
        {
            var address = account.Address();
            var playerAddressBytes = Enumerable.Range(2, playerAddress.Length - 2)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(playerAddress.Substring(x, 2), 16))
                .ToArray();
        }

        public void TestAccountChainId()
        {
            var chainId = account.ChainId();

            // check chainid?
        }

        public void TestAccountSetBlockId()
        {
            var blockId = new dojo.BlockId
            {
                tag = dojo.BlockId_Tag.BlockTag_,
                block_tag = dojo.BlockTag.Pending
            };

            account.SetBlockId(blockId);
        }

        public void TestAccountExecuteRaw()
        {
            dojo.Call call = new dojo.Call()
            {
                to = actionsAddress,
                selector = "spawn"
            };

            account.ExecuteRaw(new[] { call });

            // We wait until our callback is called to mark our 
            // entity as updated. We timeout after 5 seconds.
            var start = DateTime.Now;
            
            print(entityUpdated);
            print(modelEntityUpdated);
            
            while (!(entityUpdated || modelEntityUpdated) && DateTime.Now - start < TimeSpan.FromSeconds(5))
            {
            }


            if (entityUpdated != modelEntityUpdated)
            {
                Debug.LogWarning("Entity update status mismatch. One of the callbacks was not called.");
                Debug.LogWarning("entityUpdated != modelEntityUpdated");
            }
        }

        public void TestAccountExecuteRight()
        {
            var rightElement = dojo.felt_from_hex_be(CString.FromString("0x1"));
            if (rightElement.tag == dojo.Result_FieldElement_Tag.Err_FieldElement)
            {
                throw new Exception(rightElement.err.message);
            }
            
            dojo.Call call = new dojo.Call()
            {
                to = actionsAddress,
                selector = "move",
                calldata = new[] { rightElement.ok }
            };
            //
            account.ExecuteRaw(new[] { call });

            // We wait until our callback is called to mark our 
            // entity as updated. We timeout after 5 seconds.
            var start = DateTime.Now;
            while (!(entityUpdated || modelEntityUpdated) && DateTime.Now - start < TimeSpan.FromSeconds(5))
            {
            }
            
            
            if (entityUpdated != modelEntityUpdated)
            {
                Debug.LogWarning("Entity update status mismatch. One of the callbacks was not called.");
                Debug.LogWarning("entityUpdated != modelEntityUpdated");
            }
        }
    }
}