using StudioForge.BlockWorld;
using StudioForge.Engine;
using StudioForge.Engine.GamerServices;
using StudioForge.Engine.Integration;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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

        List<GlobalPoint3D> breakLoc; //= new List<GlobalPoint3D>();
        List<GlobalPoint3D> searchedLoc;// = new List<GlobalPoint3D>();
        List<GlobalPoint3D> FloodSearchList;

        int grid = 2; //less then 2 doesnt work

        ConfigFile lumberJackToolsCfg;
        ConfigFile lumberJackBlocksCfg;
        ConfigFile veinMineToolsCfg;
        ConfigFile veinMineOresCfg;
        ConfigFile harvestToolsCfg;
        //ConfigFile harvestBlockAuxCfg;

        int currentSearchCount = 0;
        //to be set by config file
        //Trees
        bool LumberJack = false;
        int maxTreeSearch = 10000;
        bool degradeAx = false;
       // bool animatedLumber = true;

        //mining
        bool VeinMining = false;
        int maxOreSearch = 100;
        Block previousBlock = Block.None;
        bool degradePick = false;

        //harvest
        bool Harvesting = false;
        int maxHarvest = 100;
        bool degradeScythe = false;

        ushort removeDur = 0;
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
            // harvestBlockAuxCfg = new ConfigFile("HarvestBlockAux.cfg");

            breakLoc = new List<GlobalPoint3D>();
            searchedLoc = new List<GlobalPoint3D>();
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
            //if (!File.Exists(harvestBlockAuxCfg.pathFileName))
            //{
            //    BuildDefaultHarvestBlockConfig();
            //}

            LoadAllConfigs();

            RetrieveConfigData();



        }

        private void LoadAllConfigs()
        {
            lumberJackToolsCfg.LoadConfig();
            lumberJackBlocksCfg.LoadConfig();

            veinMineToolsCfg.LoadConfig();
            veinMineOresCfg.LoadConfig();

            harvestToolsCfg.LoadConfig();
            //harvestBlockAuxCfg.LoadConfig();
        }

        public bool HandleInput(ITMPlayer player)
        {
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

            if (IsBlockWood(block) && IsAxeValid(hand.ItemID) && LumberJack)
            {
                DoLumberJack(point, hand);
            }

            if (IsBlockMineable(block) && IsPickAxeValid(hand.ItemID) && VeinMining)
            {


                DoVeinMine(point, hand);
            }

            if (IsBlockCrop(block) && IsScythValid(hand.ItemID) && Harvesting)
            {
                DoHarvest(point, hand);
            }

            ProcessErrorMessages();

        }

        private void ConsoleCommand(string str, ITMGame game, ITMPlayer player1, ITMPlayer player2, IOutputLog log)
        {
            str = str.ToLower();
            string[] command = str.Split(' ');

            if (command.Length > 1)
            {
                switch (command[1])
                {
                    case "toggleveinmine": ToggleVeinMining(); break;
                    case "togglelumberjack": ToggleLumberJack(); break;
                    case "toggleharvest": ToggleHarvesting(); break;
                    case "saveconfig": SaveAllConfig(); break;
                    default: game.AddNotification("Unknown Command"); break;
                }
            }

        }

        void ToggleVeinMining()
        {
            VeinMining = !VeinMining;
            veinMineToolsCfg.UpdateKey("VeinMining", VeinMining.ToString());
            veinMineToolsCfg.SaveConfig();
            game.AddNotification("VeinMine=" + VeinMining);
        }
        void ToggleLumberJack()
        {
            LumberJack = !LumberJack;
            lumberJackToolsCfg.UpdateKey("LumberJack", LumberJack.ToString());
            lumberJackToolsCfg.SaveConfig();
            game.AddNotification("LumberJack=" + LumberJack);
        }
        void ToggleHarvesting()
        {
            Harvesting = !Harvesting;
            harvestToolsCfg.UpdateKey("Harvesting", Harvesting.ToString());
            harvestToolsCfg.SaveConfig();
            game.AddNotification("Harvest=" + Harvesting);
        }

        private void SaveAllConfig()
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
            //if (harvestBlockAuxCfg.playerMsg != string.Empty)
            //{
            //    game.AddNotification("CFG MSG 6: " + harvestBlockAuxCfg.playerMsg, NotifyRecipient.Local);
            //    harvestBlockAuxCfg.playerMsg = string.Empty;
            //}
        }

        private void DoLumberJack(GlobalPoint3D point, ITMHand hand)
        {

            var gamerID = hand.Player.GamerID;
            breakLoc = new List<GlobalPoint3D>();
            searchedLoc = new List<GlobalPoint3D>();

            FloodSearchList.Add(point);

            //for (int x = point.X - grid; x <= point.X + grid; x++) //3x3x3 grid x value
            //{
            //    for (int y = point.Y - grid; y <= point.Y + grid; y++)
            //    {
            //        for (int z = point.Z - grid; z <= point.Z + grid; z++)
            //        {
            //            currentSearchCount++;
            //            GlobalPoint3D curPoint = new GlobalPoint3D(x, y, z);
            //            Block here = map.GetBlockID(curPoint);
            //            searchedLoc.Add(curPoint); //save this point we searched here alread
            //            if (IsBlockChopable(here))
            //            {
            //                removeDur++;
            //                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);

            //                // breakLoc.Add(curPoint); //save this point its part of a tree
            //                FloodSearchList.Add(curPoint);
            //            }

            //        }
            //    }
            //}

            //for (int i = 0; i < breakLoc.Count; i++)
            for (int i = 0; i < FloodSearchList.Count; i++)
            {
                if (currentSearchCount <= maxTreeSearch && !degradeAx)
                {//not degrading tools
                    FloodSearchTree(FloodSearchList[i], gamerID);
                    //SeachAdjacentTreeBlocks(breakLoc[i], gamerID);
                    currentSearchCount++;

                    if (degradeAx)
                    {
                        DecreaseHandItemDurability(hand, removeDur);
                    }
                    removeDur = 0;

                    currentSearchCount = 0;
                }
                else if (currentSearchCount <= maxTreeSearch && removeDur <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    //game.AddNotification("Durability remain: " + GetRemainHandDurability(hand) + " remove " + removeDur);
                    FloodSearchTree(FloodSearchList[i], gamerID);
                    // SeachAdjacentTreeBlocks(breakLoc[i], gamerID);
                    currentSearchCount++;

                    if (degradeAx)
                    {
                        DecreaseHandItemDurability(hand, removeDur);
                    }
                    removeDur = 0;

                    currentSearchCount = 0;
                }
            }

 
        }

        private void DoVeinMine(GlobalPoint3D point, ITMHand hand)
        {

            GamerID gamerID = hand.Player.GamerID;
            breakLoc = new List<GlobalPoint3D>();
            searchedLoc = new List<GlobalPoint3D>();

            for (int x = point.X - grid; x <= point.X + grid; x++) //3x3x3 grid x value
            {
                for (int y = point.Y - grid; y <= point.Y + grid; y++)
                {
                    for (int z = point.Z - grid; z <= point.Z + grid; z++)
                    {
                        currentSearchCount++;
                        GlobalPoint3D curPoint = new GlobalPoint3D(x, y, z);
                        Block here = map.GetBlockID(curPoint);

                        searchedLoc.Add(curPoint); //save this point we searched here alread

                        if (previousBlock == Block.None && IsBlockMineable(here))
                        {
                            //should only run once
                            //there is no previous block but this is a mineable spot
                            previousBlock = here;
                            if (!IsWaterNear(curPoint) && !IsLavaNear(curPoint))
                            {
                                removeDur++;

                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                breakLoc.Add(curPoint); //save this point its part of a vein
                            }
                            //TODO message player about lava/water?

                        }

                        if (IsBlockMineable(here) && here == previousBlock)
                        {
                            //there is a previouse block that matches this one and its mineable
                            if (!IsWaterNear(curPoint) && !IsLavaNear(curPoint))
                            {
                                removeDur++;

                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                breakLoc.Add(curPoint); //save this point its part of a vein
                            }
                        }

                    }
                }
            }

            for (int i = 0; i < breakLoc.Count; i++)
            {
                if (currentSearchCount <= maxOreSearch && !degradePick)
                {//not degrading do this one
                    SeachAdjacentMineBlocks(breakLoc[i], gamerID);

                }
                else if (currentSearchCount <= maxOreSearch && removeDur <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    SeachAdjacentMineBlocks(breakLoc[i], gamerID);
                    currentSearchCount++;
                }
            }

            if (degradePick)
            {
                DecreaseHandItemDurability(hand, removeDur);
            }
            removeDur = 0;

            currentSearchCount = 0;
            previousBlock = Block.None;
        }

        private void DoHarvest(GlobalPoint3D point, ITMHand hand)
        {
            var gamerID = hand.Player.GamerID;
            for (int x = point.X - grid; x <= point.X + grid; x++) //3x1x3 grid x value
            {
                for (int z = point.Z - grid; z <= point.Z + grid; z++)
                {
                    // game.AddNotification("DoHarvest");
                    currentSearchCount++;
                    GlobalPoint3D curPoint = new GlobalPoint3D(x, point.Y, z);
                    Block here = map.GetBlockID(curPoint);
                    searchedLoc.Add(curPoint); //save this point we searched here already
                    //game.AddNotification("Do curPoint " + curPoint.X + "," + curPoint.Y + "," + curPoint.Z);
                    if (IsReadyToHarvest(here, map.GetAuxData(curPoint)))
                    {
                        removeDur++;
                        map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                        breakLoc.Add(curPoint); //save this point its part of a crop
                    }

                }

            }
            //game.AddNotification("maxHarvest " + maxHarvest);
            for (int i = 0; i < breakLoc.Count; i++)
            {
                if (currentSearchCount <= maxHarvest && !degradeScythe)
                {//degrade is off so run this one
                    SeachAdjacentCropBlocks(breakLoc[i], gamerID);
                    currentSearchCount++;
                }
                else if (currentSearchCount <= maxHarvest && removeDur <= GetRemainHandDurability(hand))
                {//degrade is on so run this one but stop when tool will break
                    SeachAdjacentCropBlocks(breakLoc[i], gamerID);
                    currentSearchCount++;
                }
            }

            if (degradeScythe)
            {
                DecreaseHandItemDurability(hand, removeDur);
            }
            removeDur = 0;

            currentSearchCount = 0;
        }

        void SeachAdjacentTreeBlocks(GlobalPoint3D atPoint, GamerID gamerID)
        {
            for (int x = atPoint.X - 1; x < atPoint.X + 2; x++) //3x3x3 grid x value
            {
                for (int y = atPoint.Y - 1; y < atPoint.Y + 2; y++)
                {
                    for (int z = atPoint.Z - 1; z < atPoint.Z + 2; z++)
                    {
                        currentSearchCount++;
                        GlobalPoint3D curPoint = new GlobalPoint3D(x, y, z);
                        if (!searchedLoc.Contains(curPoint))
                        {
                            searchedLoc.Add(curPoint);
                            Block here = map.GetBlockID(curPoint);
                            if (IsBlockChopable(here))
                            {
                                removeDur++;

                                map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                // BreakBlock(here);
                                breakLoc.Add(curPoint);
                                // SeachAdjacentBlocks(curPoint);

                            }

                        }
                    }

                }
            }
            //game.AddNotification("Wood blocks found: " + treeLoc.Count, NotifyRecipient.Local);
        }
        void SeachAdjacentMineBlocks(GlobalPoint3D atPoint, GamerID gamerID)
        {
            for (int x = atPoint.X - 1; x < atPoint.X + 2; x++) //3x3x3 grid x value
            {
                for (int y = atPoint.Y - 1; y < atPoint.Y + 2; y++)
                {
                    for (int z = atPoint.Z - 1; z < atPoint.Z + 2; z++)
                    {
                        GlobalPoint3D curPoint = new GlobalPoint3D(x, y, z);
                        currentSearchCount++;
                        if (!searchedLoc.Contains(curPoint))
                        {
                            //not yet searched
                            searchedLoc.Add(curPoint);
                            Block here = map.GetBlockID(curPoint);
                            if (IsBlockMineable(here) && here == previousBlock)
                            {
                                if (!IsWaterNear(curPoint) && !IsLavaNear(curPoint))
                                {
                                    removeDur++;
                                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                                    breakLoc.Add(curPoint);
                                    currentSearchCount++;
                                }
                            }

                        }
                    }

                }
            }

        }
        void SeachAdjacentCropBlocks(GlobalPoint3D atPoint, GamerID gamerID)
        {
            for (int x = atPoint.X - 1; x < atPoint.X + 2; x++) //3x3x3 grid x value
            {
                for (int z = atPoint.Z - 1; z < atPoint.Z + 2; z++)
                {
                    currentSearchCount++;
                    GlobalPoint3D curPoint = new GlobalPoint3D(x, atPoint.Y, z);
                    // game.AddNotification("Adj curPoint " + curPoint.X + "," + curPoint.Y + "," + curPoint.Z);
                    if (!searchedLoc.Contains(curPoint))
                    {
                        searchedLoc.Add(curPoint);
                        Block here = map.GetBlockID(curPoint);
                        // game.AddNotification("curPoint " + curPoint.X + "," + curPoint.Y + "," + curPoint.Z);
                        if (IsReadyToHarvest(here, map.GetAuxData(curPoint)))
                        {
                            removeDur++;
                            map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                            breakLoc.Add(curPoint);

                        }

                    }
                }

            }

        }

        //void DecreaseDurability(ITMPlayer player, ushort amt)
        //{

        //    var item = player.Inventory.Items[player.RightHand.HandIndex];
        //    --item.Durability;
        //    item.Durability -= amt;
        //    player.Inventory.Items[player.RightHand.HandIndex] = item;

        //}

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
        private bool IsReadyToHarvest(Block block, byte aux)
        {
            //game.AddNotification("Aux is " + aux);
            if (block == Block.Crop && aux == 5)
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

            LumberJack = lumberJackToolsCfg.GetBoolEntry("LumberJack");
            VeinMining = veinMineToolsCfg.GetBoolEntry("VeinMining");
            Harvesting = harvestToolsCfg.GetBoolEntry("Harvesting");

            degradeAx = lumberJackToolsCfg.GetBoolEntry("DegradeTools");
            degradePick = veinMineToolsCfg.GetBoolEntry("DegradeTools");
            degradeScythe = harvestToolsCfg.GetBoolEntry("DegradeTools");


            if (lumberJackToolsCfg.GetIntEntry("MaxSearchBlock") > 0)
            {
                maxTreeSearch = lumberJackToolsCfg.GetIntEntry("MaxSearchBlock");
            }
            else
            {
                maxTreeSearch = 10000;
            }

            if (veinMineToolsCfg.GetIntEntry("MaxSearchBlock") > 0)
            {
                maxOreSearch = veinMineToolsCfg.GetIntEntry("MaxSearchBlock");
            }
            else
            {
                maxOreSearch = 1000;
            }

            if (harvestToolsCfg.GetIntEntry("MaxSearchBlock") > 0)
            {
                maxHarvest = harvestToolsCfg.GetIntEntry("MaxSearchBlock");
            }
            else
            {
                maxHarvest = 1000;
            }
        }

        private void BuildDefaultLumberJackToolConfig()
        {
            //add a value to the config file
            //lumberjack configs
            lumberJackToolsCfg.AddConfigKey("MaxSearchBlock", 10000.ToString());
            lumberJackToolsCfg.AddConfigKey("LumberJack", "true");
            veinMineToolsCfg.AddConfigKey("DegradeTools", "true");
            lumberJackToolsCfg.AddConfigKey(Item.WoodHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.IronHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.SteelHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.GreenstoneGoldHatchet.ToString(), "true");
            lumberJackToolsCfg.AddConfigKey(Item.DiamondHatchet.ToString(), "true");
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

        private void BuildDefaultMiningToolsConfig()
        {
            //Mining configs
            veinMineToolsCfg.AddConfigKey("MaxSearchBlock", 1000.ToString());
            veinMineToolsCfg.AddConfigKey("VeinMining", "true");
            veinMineToolsCfg.AddConfigKey("DegradeTools", "true");
            veinMineToolsCfg.AddConfigKey(Item.WoodPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.IronPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.SteelPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.GreenstoneGoldPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.DiamondPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.RubyPickaxe.ToString(), "true");
            veinMineToolsCfg.AddConfigKey(Item.TitaniumPickaxe.ToString(), "true");
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

        private void BuildDefaultHarvestToolConfig()
        {
            //add a value to the config file
            //harvest configs
            harvestToolsCfg.AddConfigKey("MaxSearchBlock", 1000.ToString());
            harvestToolsCfg.AddConfigKey("Harvesting", "true");
            veinMineToolsCfg.AddConfigKey("DegradeTools", "true");
            harvestToolsCfg.AddConfigKey(Item.BronzeScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.IronScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.SteelScythe.ToString(), "true");
            harvestToolsCfg.AddConfigKey(Item.DiamondScythe.ToString(), "true");

        }



        //private void BuildDefaultHarvestBlockConfig()
        //{
        //    //harvest crop blocks
        //    harvestBlockAuxCfg.AddConfigKey(Block.Crop.ToString(), "13");//whear
        //    harvestBlockAuxCfg.AddConfigKey(Block.Crop.ToString(), "29");//sugar
        //    harvestBlockAuxCfg.AddConfigKey(Block.Crop.ToString(), "45");//tomatoe
        //    harvestBlockAuxCfg.AddConfigKey(Block.Crop.ToString(), "61");//potatoe
        //    harvestBlockAuxCfg.AddConfigKey(Block.Crop.ToString(), "77");//corn
        //}

        void FloodSearchTree(GlobalPoint3D point, GamerID gamerID)
        {
            GlobalPoint3D curPoint;
            //north x+
            curPoint = new GlobalPoint3D(point.X + 1, point.Y, point.Z);
            if (IsBlockChopable(map.GetBlockID(curPoint)))
            {
                if (!FloodSearchList.Contains(curPoint))
                {
                    FloodSearchList.Add(curPoint);
                    map.ClearBlock(curPoint, UpdateBlockMethod.PlayerRelated, gamerID, true);
                    removeDur++;
                    currentSearchCount++;
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
                    removeDur++;
                    currentSearchCount++;
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
                    removeDur++;
                    currentSearchCount++;
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
                    removeDur++;
                    currentSearchCount++;
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
                    removeDur++;
                    currentSearchCount++;
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
                    removeDur++;
                    currentSearchCount++;
                }
            }

        }


    }
}
