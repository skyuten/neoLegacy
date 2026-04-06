#pragma once

#include "../Minecraft.World/WeighedTreasure.h"
#include "../Minecraft.World/ItemInstance.h"
#include "net.minecraft.world.item.h"
#include <unordered_map>
#include <memory>

enum CatchType {
  FISH,
  TREASURE,
  JUNK
};

class CatchTypeWeighedItem : public WeighedRandomItem {
	protected:
		CatchType type;

	public:
		CatchTypeWeighedItem(CatchType type, int weight) : WeighedRandomItem(weight)
		{
			this->type = type;
		}

		CatchType getType()
		{
			return type;
		}
};

class CatchWeighedItem : public WeighedRandomItem {
	protected:
		int itemId;
		int count;
		int auxValue;

	public:
		CatchWeighedItem(int itemId, int count, int auxValue, int weight) : WeighedRandomItem(weight)
		{
			this->itemId = itemId;
			this->count = count;
			this->auxValue = auxValue;
		}
		int getItemId()
		{
			return this->itemId;
		}
		int getCount()
		{
			return this->count;
		}
		int getAuxValue()
		{
			return this->auxValue;
		}
};

class FishingHelper
{
	private:
		FishingHelper();
		Random* random;

		WeighedRandomItemArray level0Array;
		WeighedRandomItemArray level1Array;
		WeighedRandomItemArray level2Array;
		WeighedRandomItemArray level3Array;

		WeighedRandomItemArray fishingFishArray;
		WeighedRandomItemArray fishingJunkArray;
		WeighedRandomItemArray fishingTreasuresArray;
	public:
		// Setup singleton
		FishingHelper(const FishingHelper&) = delete;
		FishingHelper& operator=(const FishingHelper&) = delete;
		static FishingHelper* getInstance();

		CatchType getRandCatchType(int level);
		CatchWeighedItem* getRandCatch(CatchType catchType);
		std::shared_ptr<ItemInstance> handleCatch(CatchWeighedItem* weighedCatch, CatchType catchType);
};