﻿using MonomiPark.SlimeRancher.Persist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMP.Networking.Component
{
    public class NetworkAmmo : Ammo
    {
        public int GetSlotIDX(Identifiable.Id id)
        {
            bool isSlotNull = false;
            bool IsIdentAllowedForAmmo = false;
            bool isSlotEmptyOrSameType = false;
            bool isSlotFull = false;
            for (int j = 0; j < ammoModel.usableSlots; j++)
            {
                isSlotNull = Slots[j] == null;

                isSlotEmptyOrSameType = isSlotNull || Slots[j].id == id;

                IsIdentAllowedForAmmo = slotPreds[j](id) && potentialAmmo.Contains(id);

                if (!isSlotNull)
                    isSlotFull = Slots[j].count >= ammoModel.slotMaxCountFunction(id, j);
                else
                    isSlotFull = false;

                if (isSlotEmptyOrSameType && isSlotFull) break;

                if (isSlotEmptyOrSameType && IsIdentAllowedForAmmo)
                {
                    return j;
                }
            }
            return -1;
        }
        public static Slot[] SRMPAmmoDataToSlots(List<AmmoDataV02> ammo)
        {
            Slot[] array = new Slot[ammo.Count];
            for (int i = 0; i < ammo.Count; i++)
            {
                bool whyTheHellDoIHaveToMakeAVariableForThis = ammo[i] == null || ammo[i].id == Identifiable.Id.NONE || ammo[i].count == 0;
                if (whyTheHellDoIHaveToMakeAVariableForThis)
                {
                    array[i] = null;
                    continue;
                }

                array[i] = new Slot(ammo[i].id, ammo[i].count);
                array[i].emotions = new SlimeEmotionData();
                foreach (KeyValuePair<SlimeEmotions.Emotion, float> emotionDatum in ammo[i].emotionData.emotionData)
                {
                    array[i].emotions[emotionDatum.Key] = emotionDatum.Value;
                }
            }

            return array;
        }

        internal void ClientFixedUpdate()
        {
            for (var i = 0; i > Slots.Length; i++)
            {
                Slot slot = Slots[i];
                if (slot.count <= 0)
                {
                    Slots[i] = null;
                }
            }
        }

        /// <summary>
        /// Site ID -> Ammo
        /// </summary>
        public static Dictionary<string, Ammo> all = new Dictionary<string, Ammo>();

        public string ammoId;
        public NetworkAmmo(string id, HashSet<Identifiable.Id> potentialAmmo, int numSlots, int usableSlots, Predicate<Identifiable.Id>[] slotPreds, Func<Identifiable.Id, int, int> slotMaxCountFunction) : base(potentialAmmo, numSlots, usableSlots, slotPreds, slotMaxCountFunction)
        {
            ammoId = id;
            all.Add(ammoId, this);

            // Create ammo model... For having a model to start with!!??
            ammoModel = new AmmoModel()
            {
                slots = new Slot[numSlots],
                usableSlots = usableSlots,
                slotMaxCountFunction = slotMaxCountFunction
            };
        }
    }
}
