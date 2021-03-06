using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StudioForge.BlockWorld;
using StudioForge.Engine;
using StudioForge.Engine.Core;
using StudioForge.Engine.GamerServices;
using StudioForge.Engine.Integration;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace QuickGather
{
    public class QuickGatherMod : ITMPlugin
    {
        #region ITMPlugin


        void ITMPlugin.WorldSaved(int version)
        {
        }

        void ITMPlugin.PlayerJoined(ITMPlayer player)
        {
        }

        void ITMPlugin.PlayerLeft(ITMPlayer player)
        {
        }

        void ITMPlugin.UnloadMod()
        {

        }
        #endregion

        #region Member Variables

        public static string Path;
        ITMGame game;
        ITMMap map;
        ITMWorld world;

        List<GlobalPoint3D> FloodSearchList;


        ConfigFile lumberJackToolsCfg;
        ConfigFile lumberJackBlocksCfg;
        ConfigFile veinMineToolsCfg;
        ConfigFile veinMineOresCfg;
        ConfigFile harvestToolsCfg;


        int amountFound = 0;
        bool keyDown = false;
        Keys toggleKey = Keys.Q;

        //to be set by config file
        //Trees
        bool LumberJack = false;
        int maxTreeToFind = 10000;
        bool degradeAx = false;
        // bool animatedLumber = true;

        //mining
        bool VeinMining = false;
        int maxOreToFind = 100;
        Block targetOre = Block.None;
        bool degradePick = false;
        int mineGrid = 1;
        bool sameOreOnly = true;

        //harvest
        bool Harvesting = false;
        int maxHarvestToFind = 100;
        bool degradeScythe = false;

        ushort durabilityToRemove = 0;
        // private float notifyElapsed;
        #endregion


        public void Initialize(ITMPluginManager mgr, string path)
        {
            Path = path;
        }

        public void InitializeGame(ITMGame game)
        {


            lumberJackToolsCfg = new ConfigFile("LumberJack.cfg");
            lumberJackBlocksCfg = new ConfigFile("LumberJackBlocks.cfg");

            veinMineToolsCfg = new ConfigFile("VeinMine.cfg");
            veinMineOresCfg = new ConfigFile("VeinMineBlocks.cfg");

            harvestToolsCfg = new ConfigFile("Harvest.cfg");


            FloodSearchList = new List<GlobalPoint3D>();


            this.game = game;
            world = game.World;
            map = world.Map;
            game.AddEventBlockMined(Block.None, MyAction);
            game.AddConsoleCommand(ConsoleCommand, "qg", "runs specified command", "Commands: ToggleVeinMine, ToggleLumberJack, ToggleHarvest");


            if (!File.Exists(lumberJackToolsCfg.pathFileName))
            {
                BuildDefaultLumberJackToolConfig();
            }
            if (!File.Exists(lumberJackBlocksCfg.pathFileName))
            {
                BuildDefaultLumberJackBlockConfig();
            }

            if (!File.Exists(veinMineToolsCfg.pathFileName))
            {
                BuildDefaultMiningToolsConfig();
            }
            if (!File.Exists(veinMineOresCfg.pathFileName))
            {
                BuildDefaultMiningBlocksConfig();
            }

            if (!File.Exists(harvestToolsCfg.pathFileName))
            {
                BuildDefaultHarvestToolConfig();
            }

            LoadAllConfigs();
            RetrieveConfigData();



        }

        private void LoadAllConfigs()
        {
            //config.LoadConfig();

            lumberJackToolsCfg.LoadConfig();
            lumberJackBlocksCfg.LoadConfig();

            veinMineToolsCfg.LoadConfig();
            veinMineOresCfg.LoadConfig();

            harvestToolsCfg.LoadConfig();

        }

        public bool HandleInput(ITMPlayer player)
        {

            //bool qPressed = InputManager.IsKeyPressed(PlayerIndex.One, Keys.Q);
            if (InputManager.IsKeyPressed(player.PlayerIndex, toggleKey))
            {
                keyDown = true;
            }
            else
            {
                keyDown = false;
            }
            //handle special mod input keys
            return false;
        }

        public void Update()
        {
            //notifyElapsed += Services.ElapsedTime;
            //if (notifyElapsed > 0.5)
            //{
            //    int remainLoops = 10;
            //    if (FloodSearchList.Count > 0 && animatedLumber && remainLoops > 0)
            //    {

            //        //working here on chopping a certain amount of trees per fram




            //        remainLoops--;
            //    }
            //    notifyElapsed = 0;
            //}

        }
        public void Update(ITMPlayer player)
        {

        }
        public void Draw(ITMPlayer player, ITMPlayer virtualPlayer)
        {
        }

        void MyAction(Block block, byte aux, GlobalPoint3D point, ITMHand hand)
        {

            if (!keyDown)
            {
                if (IsBlockWood(block) && IsAxeValid(hand.ItemID) && LumberJack)
                {
                    DoLumberJack(point, hand);
                }

                if (IsBlockMineable(block) && IsPickAxeValid(hand.ItemID) && VeinMining)
                {
                    targetOre = block;
                    DoVeinMine(point, hand);
                }

                if (IsBlockCrop(block) && IsScythValid(hand.ItemID) && Harvesting)
                {
                    DoHarvest(point, hand);
                }

                ProcessErrorMessages();
            }

        }

        private void ConsoleCommand(string str, ITMGame game, ITMPlayer player1, ITMPlayer player2, IOutputLog log)
        {
            str = str.ToLower();
            string[] commands = str.Split(' ');

            if (commands.Length > 1)
            {
                switch (commands[1])
                {
                    case "help": log.WriteLine(QuickGatherHelp(commands)); break;
                    case "toggleveinmine": log.WriteLine(ToggleVeinMining()); break;
                    case "togglelumberjack": log.WriteLine(ToggleLumberJack()); break;
                    case "toggleharvest": log.WriteLine(ToggleHarvesting()); break;
                    case "saveconfig": log.WriteLine(SaveConfigs()); break;
                    case "ver": log.WriteLine(GetVersion()); break;
                    case "version": log.WriteLine(GetVersion()); break;
                    default: log.WriteLine("Unknown Command use 'qg help' for more info"); break;
                }

            }
            else
            {
                log.WriteLine("Unknown Command use 'qg help' for help");
            }


        }

        #region ConsoleCommands
        string ToggleVeinMining()
        {
            VeinMining = !VeinMining;
            veinMineToolsCfg.UpdateKey("VeinMining", VeinMining.ToString());
            veinMineToolsCfg.SaveConfig();
            return "VeinMine  is now set to " + VeinMining;
            //game.AddNotification("VeinMine=" + VeinMining);
        }
        string ToggleLumberJack()
        {
            LumberJack = !LumberJack;
            lumberJackToolsCfg.UpdateKey("LumberJack", LumberJack.ToString());
            lumberJackToolsCfg.SaveConfig();
            return "LumberJack is now set to " + LumberJack;
            //game.AddNotification("LumberJack=" + LumberJack);
        }
        string ToggleHarvesting()
        {
            Harvesting = !Harvesting;
            harvestToolsCfg.UpdateKey("Harvesting", Harvesting.ToString());
            harvestToolsCfg.SaveConfig();
            return "Harvest is now set to " + Harvesting;
            // game.AddNotification("Harvest=" + Harvesting);
        }
        string QuickGatherHelp(string[] commands)
        {
            //0: initial console command
            //1: first help command help only
            //2: second help command help for indivitual command
            string str = "";
            if (commands.Length == 2)
            {
                str = "Help: use 'Help <command>' for more info\n" +
                    "ToggleVeinMine, ToggleLumberJack, ToggleHarvest,\n" +
                    "SaveConfig, ver/version";
                return str;
            }
            if (commands.Length > 2)
            {
                switch (commands[2])
                {
                    case "toggleveinmine": str = "ToggleVeinMine: toggles vein mining on and off"; break;
                    case "togglelumberjack": str = "ToggleLumberJack: toggles lumberjack on and off"; break;
                    case "toggleharvest": str = "ToggleHarvest: toggle harvesting on and off"; break;
                    case "saveconfig": str = "SaveConfig: save changes to config file"; break;
                    case "ver": str = "Display current version"; break;
                    case "version": str = "Display current version"; break;
                    default: str = "Unknown Command"; break;
                }
            }
            return str;
        }
        string SaveConfigs()
        {
            SaveAllConfig();
            return "Config saved";

        }
        string GetVersion()
        {

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            //Version version = Assembly.GetEntryAssembly().GetName().Version;

            string str = "Version: " + version;
            return str;
        }


        #endregion
        void GetCustomInputKey()
        {
            if (lumberJackToolsCfg.ContainsEntry("ToggleKey"))
            {
                string str = lumberJackToolsCfg.GetKeyString("ToggleKey");
                if (!System.Enum.TryParse<Keys>(str, out toggleKey))
                {
                    //tryparse failed
                    toggleKey = Keys.Q;
                }

                //toggleKey = (Keys)System.Enum.Parse(typeof(Keys), str);

            }
            else
            {
                lumberJackToolsCfg.AddConfigKey("ToggleKey", "Q");
            }

        }

        void SaveAllConfig()
        {
            //need to update dictionary first
            lumberJackToolsCfg.SaveConfig();
            lumberJackBlocksCfg.SaveConfig();

            veinMineToolsCfg.SaveConfig();
            veinMineOresCfg.SaveConfig();

            harvestToolsCfg.SaveConfig();


        }

        private void ProcessErrorMessages()
        {
            if (lumberJackToolsCfg.playerMsg != string.Empty)
            {
                game.AddNotification("CFG MSG 1: " + lumberJackToolsCfg.playerMsg, NotifyRecipient.Local);
                lumberJackToolsCfg.playerMsg = string.Empty;
            }
            if (lumberJackBlocksCfg.playerMsg != string.Empty)
            {
                game.AddNotification("CFG MSG 2: " + lumberJackBlocksCfg.playerMsg, NotifyRecipient.Local);
                lumberJackBlocksCfg.playerMsg = string.Empty;
            }
            if (veinMineToolsCfg.playerMsg != string.Empty)
            {
                game.AddNotification("CFG MSG 3: " + veinMineToolsCfg.playerMsg, NotifyRecipient.Local);
                veinMineToolsCfg.playerMsg = string.Empty;
            }
            if (veinMineOresCfg.playerMsg != string.Empty)
            {
                game.AddNotification("CFG MSG 4: " + veinMineOresCfg.playerMsg, NotifyRecipient.Local);
                veinMineOresCfg.playerMsg = string.Empty;
            }
            if (harvestToolsCfg.playerMsg != string.Empty)
            {
                game.AddNotification("CFG MSG 5: " + harvestToolsCfg.playerMsg, NotifyRecipient.Local);
                harvestToolsCfg.playerMsg = string.Empty;
            }

        }

        private void DoLumberJack(GlobalPoint3D point, ITMHand hand)
        {

            var gamerID = hand.Player.GamerID;

            FloodSearchList = new List<GlobalPoint3D>();
            FloodSearchList.Add(point);

            for (int i = 0; i < FloodSearchList.Count; i++)
            {
                if (amountFound <= maxTreeToFind && !degradeAx)
                {//not degrading tools
                    FloodSearchTree(FloodSearchList[i], gamerID);
                    amountFound++;

                }
                else if (amountFound <= maxTreeToFind && durabilityToRemove <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    FloodSearchTree(FloodSearchList[i], gamerID);
                    amountFound++;
                }

            }
            //game.AddNotification("dur: " + GetRemainHandDurability(hand) + " to remove " + durabilityToRemove);
            if (degradeAx)
            {
                DecreaseHandItemDurability(hand, durabilityToRemove);
            }
            durabilityToRemove = 0;

            amountFound = 0;


        }

        private void DoVeinMine(GlobalPoint3D point, ITMHand hand)
        {
            //game.AddNotification("DoVeinMin");
            GamerID gamerID = hand.Player.GamerID;
            //setup the initial search pattern
            FloodSearchList = new List<GlobalPoint3D>();
            FloodSearchOre(point, gamerID, targetOre);

            for (int i = 0; i < FloodSearchList.Count; i++)
            {
                //game.AddNotification("FloodSearchList.Count " + FloodSearchList.Count);
                if (amountFound <= maxOreToFind && !degradePick)
                {//not degrading do this one
                    FloodSearchOre(FloodSearchList[i], gamerID, targetOre);
                }
                else if (amountFound <= maxOreToFind && durabilityToRemove <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    FloodSearchOre(FloodSearchList[i], gamerID, targetOre);
                    amountFound++;
                }
            }
            if (degradePick)
            {
                DecreaseHandItemDurability(hand, durabilityToRemove);
            }
            durabilityToRemove = 0;
            amountFound = 0;
            targetOre = Block.None;
        }

        private void DoHarvest(GlobalPoint3D point, ITMHand hand)
        {
            var gamerID = hand.Player.GamerID;
            FloodSearchList = new List<GlobalPoint3D>();
            FloodSearchCrop(point, gamerID);

            for (int i = 0; i < FloodSearchList.Count; i++)
            {
                if (amountFound <= maxHarvestToFind && !degradeScythe)
                {//degrade is off so run this one
                    FloodSearchCrop(FloodSearchList[i], gamerID);
                    amountFound++;
                }
                else if (amountFound <= maxHarvestToFind && durabilityToRemove <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    FloodSearchCrop(FloodSearchList[i], gamerID);
                    amountFound++;
                }
            }

            if (degradeScythe)
            {
                DecreaseHandItemDurability(hand, durabilityToRemove);
            }
            durabilityToRemove = 0;

            amountFound = 0;
        }

        void DecreaseHandItemDurability(ITMHand hand, ushort amt)
        {
            ITMPlayer player = hand.Player;

            var item = player.Inventory.Items[player.LeftHand.HandIndex];
            if (hand.HandType == InventoryHand.Right)
            {
                item = player.Inventory.Items[player.RightHand.HandIndex];
            }


            if (amt >= item.Durability)
            {
                //game.AddNotification("Amount " + amt + " >= item.durability " + item.Durability );
                amt = (ushort)(item.Durability - 1);
                //game.AddNotification("setting to " + amt );
            }

            item.Durability -= amt;
            // game.AddNotification("durability " + item.Durability + " amount " + amt);

            if (hand.HandType == InventoryHand.Right)
            {
                player.Inventory.Items[player.RightHand.HandIndex] = item;
            }
            else
            {
                player.Inventory.Items[player.LeftHand.HandIndex] = item;
            }

        }

        ushort GetRemainHandDurability(ITMHand hand)
        {
            ITMPlayer player = hand.Player;

            var item = player.Inventory.Items[player.LeftHand.HandIndex];
            if (hand.HandType == InventoryHand.Right)
            {
                item = player.Inventory.Items[player.RightHand.HandIndex];
            }

            return item.Durability;
        }

        bool IsAxeValid(Item id)
        {
            return lumberJackToolsCfg.GetBoolEntry(id.ToString());

        }
        bool IsBlockChopable(Block block)
        {
            return lumberJackBlocksCfg.GetBoolEntry(block.ToString());

        }
        bool IsBlockWood(Block block)
        {
            switch (block)
            {
                case Block.Wood: return true;
                case Block.BirchWood: return true;

                default: return false;
            }
        }

        bool IsPickAxeValid(Item id)
        {
            return veinMineToolsCfg.GetBoolEntry(id.ToString());

        }
        bool IsBlockMineable(Block block)
        {
            return veinMineOresCfg.GetBoolEntry(block.ToString());

        }

        private bool IsBlockCrop(Block block)
        {
            if (block == Block.Crop)
            {
                return true;
            }
            return false;
        }
        private bool IsReadyToHarvest(GlobalPoint3D point)
        {

            //game.AddNotification("Aux is " + aux);
            // if (block == Block.Crop && aux == 5)
            if (map.GetBlockID(point) == Block.Crop && map.GetAuxDataNoCache(point) == 5)
            {
                //string key = Block.Crop + "=" + aux;
                //game.AddNotification("Ready to harvest " + key +" " + harvestBlockAuxCfg.ContainsEntry(key));
                //return harvestBlockAuxCfg.ContainsEntry(key);
                return true;
            }
            return false;
        }
        bool IsScythValid(Item id)
        {
            return harvestToolsCfg.GetBoolEntry(id.ToString());

        }

        bool IsLavaNear(GlobalPoint3D point)
        {
            for (int x = point.X - 1; x <= point.X + 1; x++)
            {
                GlobalPoint3D here = new GlobalPoint3D(x, point.Y, point.Z);
                if (map.GetBlockID(here) == Block.Lava)
                {
                    return true;
                }

            }
            for (int y = point.Y - 1; y <= point.Y + 1; y++)
            {
                GlobalPoint3D here = new GlobalPoint3D(point.X, y, point.Z);
                if (map.GetBlockID(here) == Block.Lava)
                {
                    return true;
                }
            }
            for (int z = point.Z - 1; z <= point.Z + 1; z++)
            {
                GlobalPoint3D here = new GlobalPoint3D(point.X, point.Y, z);
                if (map.GetBlockID(here) == Block.Lava)
                {
                    return true;
                }
            }


            return false;
        }

        bool IsWaterNear(GlobalPoint3D point)
        {


            for (int x = point.X - 1; x <= point.X + 1; x++)
            {
                GlobalPoint3D here = new GlobalPoint3D(x, point.Y, point.Z);
                if (map.GetBlockID(here) == Block.Water)
                {
                    return true;
                }

            }
            for (int y = point.Y - 1; y <= point.Y + 1; y++)
            {
                GlobalPoint3D here = new GlobalPoint3D(point.X, y, point.Z);
                if (map.GetBlockID(here) == Block.Water)
                {
                    return true;
                }
            }
            for (int z = point.Z - 1; z <= point.Z + 1; z++)
            {
                GlobalPoint3D here = new GlobalPoint3D(point.X, point.Y, z);
                if (map.GetBlockID(here) == Block.Water)
                {
                    return true;
                }
            }


            return false;
        }

        void RetrieveConfigData()
        {
            //retrieve the value from the config and save them here for use
            //if the value doesnt exist create it

            if (lumberJackToolsCfg.ContainsEntry("LumberJack"))
            {
                LumberJack = lumberJackToolsCfg.GetBoolEntry("LumberJack");

            }
            else
            {
                lumberJackToolsCfg.AddConfigKey("LumberJack", "false");
                LumberJack = false;
            }

            if (veinMineToolsCfg.ContainsEntry("VeinMining"))
            {
                VeinMining = veinMineToolsCfg.GetBoolEntry("VeinMining");
            }
            else
            {
                VeinMining = false;
                veinMineToolsCfg.AddConfigKey("VeinMining", "false");
            }

            if (harvestToolsCfg.ContainsEntry("Harvesting"))
            {
                Harvesting = harvestToolsCfg.GetBoolEntry("Harvesting");
            }
            else
            {
                Harvesting = false;
                harvestToolsCfg.AddConfigKey("Harvesting", "false");
            }

            if (lumberJackToolsCfg.ContainsEntry("DegradeAx"))
            {
                degradeAx = lumberJackToolsCfg.GetBoolEntry("DegradeAx");
            }
            else
            {
                degradeAx = false;
                lumberJackToolsCfg.AddConfigKey("DegradeAx", "false");
            }

            if (veinMineToolsCfg.ContainsEntry("DegradePick"))
            {
                degradePick = veinMineToolsCfg.GetBoolEntry("DegradePick");
            }
            else
            {
                degradePick = false;
                veinMineToolsCfg.AddConfigKey("DegradePick", "false");
            }

            if (veinMineToolsCfg.ContainsEntry("MineGrid"))
            {
                mineGrid = veinMineToolsCfg.GetIntEntry("MineGrid");
            }
            else
            {
                mineGrid = 1;
              //hidden value players can add the key to change it but its not displayed.
                //  veinMineToolsCfg.AddConfigKey("MineGrid", 1.ToString());
            }

            if (veinMineToolsCfg.ContainsEntry("SameOreOnly"))
            {
                sameOreOnly = veinMineToolsCfg.GetBoolEntry("SameOreOnly");
            }
            else
            {
                sameOreOnly = true;

                veinMineToolsCfg.AddConfigKey("SameOreOnly", "true");
            }

            if (harvestToolsCfg.ContainsEntry("DegradeScyth"))
            {
                degradeScythe = harvestToolsCfg.GetBoolEntry("DegradeScyth");
            }
            else
            {
                degradeScythe = false;
                harvestToolsCfg.AddConfigKey("DegradeScyth", "false");
            }


            if (lumberJackToolsCfg.ContainsEntry("LumberMaxCollection"))
            {
                maxTreeToFind = lumberJackToolsCfg.GetIntEntry("LumberMaxCollection");
            }
            else
            {
                maxTreeToFind = 1000;
                lumberJackToolsCfg.AddConfigKey("LumberMaxCollection", 1000.ToString());
            }

            if (veinMineToolsCfg.ContainsEntry("MaxBlockCollection"))
            {
                maxOreToFind = veinMineToolsCfg.GetIntEntry("MaxBlockCollection");
            }
            else
            {
                maxOreToFind = 1000;
                veinMineToolsCfg.AddConfigKey("MaxBlockCollection", 1000.ToString());
            }

            if (harvestToolsCfg.ContainsEntry("MaxBlockCollection"))
            {
                maxHarvestToFind = harvestToolsCfg.GetIntEntry("MaxBlockCollection");
            }
            else
            {
                maxHarvestToFind = 1000;
                harvestToolsCfg.AddConfigKey("MaxBlockCollection", 1000.ToString());
            }

            GetCustomInputKey();
        }



        private void BuildDefaultLumberJackToolConfig()
        {
            //add a value to the config file
            //lumberjack configs
            lumberJackToolsCfg.AddConfigKey("ToggleKey", "Q");
            lumberJackToolsCfg.AddConfigKey("MaxBlockCollection", 1000.ToString());
            lumberJackToolsCfg.AddConfigKey("LumberJack", "true");
            lumberJackToolsCfg.AddConfigKey("DegradeAx", "true");
            lumberJackToolsCfg.AddConfigKey(Item.WoodHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.IronHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.SteelHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.GreenstoneGoldHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.DiamondHatchet.ToString(), "true");
        }
        private void BuildDefaultMiningToolsConfig()
        {
            //Mining configs
            veinMineToolsCfg.AddConfigKey("MaxBlockCollection", 1000.ToString());
            veinMineToolsCfg.AddConfigKey("VeinMining", "true");
            veinMineToolsCfg.AddConfigKey("DegradePick", "true");
            veinMineToolsCfg.AddConfigKey("SameOreOnly", "true");
           // veinMineToolsCfg.AddConfigKey("MineGrid", 1.ToString()); //hidden value
            veinMineToolsCfg.AddConfigKey(Item.WoodPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.IronPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.SteelPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.GreenstoneGoldPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.DiamondPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.RubyPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.TitaniumPickaxe.ToString(), "true");
        }
        private void BuildDefaultHarvestToolConfig()
        {
            //add a value to the config file
            //harvest configs
            harvestToolsCfg.AddConfigKey("MaxBlockCollection", 1000.ToString());
            harvestToolsCfg.AddConfigKey("Harvesting", "true");
            harvestToolsCfg.AddConfigKey("DegradeScyth", "true");
            harvestToolsCfg.AddConfigKey(Item.BronzeScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.IronScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.SteelScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.DiamondScythe.ToString(), "true");

        }

        private void BuildDefaultLumberJackBlockConfig()
        {
            //lumberjack blocks
            lumberJackBlocksCfg.AddConfigKey(Block.Wood.ToString(), "true");
            lumberJackBlocksCfg.AddConfigKey(Block.BirchWood.ToString(), "true");
            lumberJackBlocksCfg.AddConfigKey(Block.Leaves.ToString(), "true");
            lumberJackBlocksCfg.AddConfigKey(Block.MapleLeaves.ToString(), "true");
            lumberJackBlocksCfg.AddConfigKey(Block.PineLeaves.ToString(), "true");
        }
        private void BuildDefaultMiningBlocksConfig()
        {
            //mining blocks
            veinMineOresCfg.AddConfigKey(Block.Iron.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Cassiterite.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Coal.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Copper.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Diamond.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Gold.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.GoldBlock.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Greenstone.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Opal.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Platinum.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Ruby.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.SaltBlock.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Titanium.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Uranium.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Flint.ToString(), "true");

            veinMineOresCfg.AddConfigKey(Block.Sapphire.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Sulphur.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Carbon.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Fluorite.ToString(), "true");
            veinMineOresCfg.AddConfigKey(Block.Cyclonite.ToString(), "true");

        }



        void FloodSearchTree(GlobalPoint3D point, GamerID gamerID)
        {

            //TODO MAYBE diagional search pattern?
            GlobalPoint3D curPoint;

            //north x+
            curPoint = new GlobalPoint3D(point.X + 1, point.Y, point.Z);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }

            //south X-
            curPoint = new GlobalPoint3D(point.X - 1, point.Y, point.Z);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //east z+
            curPoint = new GlobalPoint3D(point.X, point.Y, point.Z + 1);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //west z-
            curPoint = new GlobalPoint3D(point.X, point.Y, point.Z - 1);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //up y+
            curPoint = new GlobalPoint3D(point.X, point.Y + 1, point.Z);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //dn y-
            curPoint = new GlobalPoint3D(point.X, point.Y - 1, point.Z);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }

        }

        void FloodSearchOre(GlobalPoint3D point, GamerID gamerID, Block workingOre)
        {
            GlobalPoint3D curPoint;
            Block curOre;

            for (int y = mineGrid; y >= -mineGrid; y--)
            {
               
                //north x+
                for (int i = 0; i <= mineGrid; i++)
                {
                    curPoint = new GlobalPoint3D(point.X + i, point.Y + y, point.Z);
                    curOre = map.GetBlockID(curPoint);

                    if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                    {
                        if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                        {
                            if (!FloodSearchList.Contains(curPoint))
                            {
                                FloodSearchList.Add(curPoint);
                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                durabilityToRemove++;
                                amountFound++;
                            }
                        }
                    }
                }

                //south X-
                for (int i = 0; i <= mineGrid; i++)
                {
                    curPoint = new GlobalPoint3D(point.X - i, point.Y + y, point.Z);
                    curOre = map.GetBlockID(curPoint);
                    if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                    {
                        if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                        {
                            if (!FloodSearchList.Contains(curPoint))
                            {
                                FloodSearchList.Add(curPoint);
                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                durabilityToRemove++;
                                amountFound++;
                            }
                        }
                    }
                }

                //east z+
                for (int i = 0; i <= mineGrid; i++)
                {
                    curPoint = new GlobalPoint3D(point.X, point.Y + y, point.Z + i);
                    curOre = map.GetBlockID(curPoint);

                    if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                    {
                        if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                        {
                            if (!FloodSearchList.Contains(curPoint))
                            {
                                FloodSearchList.Add(curPoint);
                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                durabilityToRemove++;
                                amountFound++;
                            }
                        }
                    }
                }

                //west z-
                for (int i = 0; i <= mineGrid; i++)
                {
                    curPoint = new GlobalPoint3D(point.X, point.Y + y, point.Z - i);
                    curOre = map.GetBlockID(curPoint);
                    if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                    {
                        if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                        {
                            if (!FloodSearchList.Contains(curPoint))
                            {
                                FloodSearchList.Add(curPoint);
                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                durabilityToRemove++;
                                amountFound++;
                            }
                        }
                    }
                }
                
            //diagonals +x,-z
            for (int i = 0; i <= mineGrid; i++)
            {
                curPoint = new GlobalPoint3D(point.X + i, point.Y + y, point.Z - i);
                curOre = map.GetBlockID(curPoint);
                if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                {
                    if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                    {
                        if (!FloodSearchList.Contains(curPoint))
                        {
                            FloodSearchList.Add(curPoint);
                            map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                            durabilityToRemove++;
                            amountFound++;
                        }
                    }
                }
            }

            //diagonals +x,+z
            for (int i = 0; i <= mineGrid; i++)
            {
                curPoint = new GlobalPoint3D(point.X + i, point.Y + y, point.Z + i);
                curOre = map.GetBlockID(curPoint);
                if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                {
                    if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                    {
                        if (!FloodSearchList.Contains(curPoint))
                        {
                            FloodSearchList.Add(curPoint);
                            map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                            durabilityToRemove++;
                            amountFound++;
                        }
                    }
                }
            }

            //diagonals -x,+z
            for (int i = 0; i <= mineGrid; i++)
            {
                curPoint = new GlobalPoint3D(point.X - i, point.Y + y, point.Z + i);
                curOre = map.GetBlockID(curPoint);
                if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                {
                    if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                    {
                        if (!FloodSearchList.Contains(curPoint))
                        {
                            FloodSearchList.Add(curPoint);
                            map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                            durabilityToRemove++;
                            amountFound++;
                        }
                    }
                }
            }

            //diagonals -x,-z
            for (int i = 0; i <= mineGrid; i++)
            {
                curPoint = new GlobalPoint3D(point.X - i, point.Y + y, point.Z - i);
                curOre = map.GetBlockID(curPoint);
                if (IsBlockMineable(map.GetBlockID(curPoint)) && !IsLavaNear(curPoint) && !IsWaterNear(curPoint))
                {
                    if (sameOreOnly && curOre == workingOre || !sameOreOnly)
                    {
                        if (!FloodSearchList.Contains(curPoint))
                        {
                            FloodSearchList.Add(curPoint);
                            map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                            durabilityToRemove++;
                            amountFound++;
                        }
                    }
                }
            }

        }      

        }

        void FloodSearchCrop(GlobalPoint3D point, GamerID gamerID)
        {


            GlobalPoint3D curPoint;

            //north x+
            curPoint = new GlobalPoint3D(point.X + 1, point.Y, point.Z);
            if (IsReadyToHarvest(curPoint))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }

            //south X-
            curPoint = new GlobalPoint3D(point.X - 1, point.Y, point.Z);
            if (IsReadyToHarvest(curPoint))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //east z+
            curPoint = new GlobalPoint3D(point.X, point.Y, point.Z + 1);
            if (IsReadyToHarvest(curPoint))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }
            //west z-
            curPoint = new GlobalPoint3D(point.X, point.Y, point.Z - 1);
            if (IsReadyToHarvest(curPoint))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    durabilityToRemove++;
                    amountFound++;
                }
            }

        }

        string PointToString(GlobalPoint3D point)
        {
            string str = point.X.ToString() + " " + point.Y.ToString() + " " + point.Z.ToString();
            return str;
        }



        #region depreciated


        #endregion
    }
}
