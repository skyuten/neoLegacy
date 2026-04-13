	#include "stdafx.h"
	#include "net.minecraft.world.level.newbiome.layer.h"
	#include "RiverInitLayer.h"
	#include "RareBiomeSpotLayer.h"
	#include "net.minecraft.world.level.h"
	#include "BiomeOverrideLayer.h"
	#include "CustomizableSourceSettings.h"

	#ifdef __PSVITA__
	// AP - this is used to perform fast 64bit divides of known values
	#include "../Minecraft.Client/PSVita/PSVitaExtras/libdivide.h"
#include <ImposeContinentsLayer.h>


	libdivide::divider<long long> fast_d2(2);
	libdivide::divider<long long> fast_d3(3);
	libdivide::divider<long long> fast_d4(4);
	libdivide::divider<long long> fast_d5(5);
	libdivide::divider<long long> fast_d6(6);
	libdivide::divider<long long> fast_d7(7);
	libdivide::divider<long long> fast_d10(10);
	#endif

LayerArray Layer::getDefaultLayers(int64_t seed, LevelType* levelType, void* superflatConfig) {
    
    
    shared_ptr<Layer> islandLayer = std::make_shared<IslandLayer>(seed, 1);
    islandLayer = std::make_shared<FuzzyZoomLayer>(seed, islandLayer, 0x7D0);
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 1);
    islandLayer = std::make_shared<ZoomLayer>(seed, islandLayer, 0x7D1);      
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 2);
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 0x32);  
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 0x46);  
    islandLayer = std::make_shared<RemoveTooMuchOceanLayer>(seed, islandLayer, 2);
    islandLayer = std::make_shared<AddSnowLayer>(seed, islandLayer, 2);
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 3);
    islandLayer = std::make_shared<AddEdgeLayer>(seed, islandLayer, 2, 0); 
    islandLayer = std::make_shared<AddEdgeLayer>(seed, islandLayer, 2, 1); 
    islandLayer = std::make_shared<AddEdgeLayer>(seed, islandLayer, 3, 2);
    islandLayer = std::make_shared<ZoomLayer>(seed, islandLayer, 0x7D2); 
    islandLayer = std::make_shared<ZoomLayer>(seed, islandLayer, 0x7D3);  
    islandLayer = std::make_shared<AddIslandLayer>(seed, islandLayer, 4);
    islandLayer = std::make_shared<AddMushroomIslandLayer>(seed, islandLayer, 5);
    islandLayer = std::make_shared<DeepOceanLayer>(seed, islandLayer, 4);

    shared_ptr<Layer> baseLayer = ZoomLayer::zoom(seed, islandLayer, 0x3E8, 0); 
    int zoomLevel = 4;
    int riverZoomCount = 4;

    if (levelType == LevelType::lvl_customized && superflatConfig != nullptr) {
        auto settings = CustomizableSourceSettings::Builder::build(
            CustomizableSourceSettings::Builder::fromString(superflatConfig));
        zoomLevel = settings->getBiomeSize();
        riverZoomCount = settings->getRiverSize();
    }
    
    if (levelType == LevelType::lvl_largeBiomes) {
        zoomLevel = 6;
    }

    
    shared_ptr<Layer> riverInit = make_shared<RiverInitLayer>(seed, baseLayer, 0x64); 
    
    
    shared_ptr<Layer> hillsNoise = ZoomLayer::zoom(seed, riverInit, 0x3E8, 2); 
    
    
    shared_ptr<Layer> riverLayerFinal = ZoomLayer::zoom(seed, riverInit, 0x3E8, 2);
    riverLayerFinal = ZoomLayer::zoom(seed, riverLayerFinal, 0x3E8, riverZoomCount);
    
    riverLayerFinal = make_shared<RiverLayer>(seed, riverLayerFinal, 1);
    riverLayerFinal = make_shared<SmoothLayer>(seed, riverLayerFinal, 0x3E8);
    
   
    shared_ptr<Layer> biomeLayer = make_shared<BiomeInitLayer>(seed, baseLayer, 0xC8, levelType, superflatConfig); 
    biomeLayer = ZoomLayer::zoom(seed, biomeLayer, 0x3E8, 2);
    biomeLayer = make_shared<BiomeEdgeLayer>(seed, biomeLayer, 0x3E8);
    biomeLayer = make_shared<RegionHillsLayer>(seed, biomeLayer, hillsNoise, 0x3E8);
    biomeLayer = make_shared<RareBiomeSpotLayer>(seed, biomeLayer, 0x3E9); 

    
    for (int i = 0; i < zoomLevel; ++i) {
        biomeLayer = make_shared<ZoomLayer>(seed, biomeLayer, 0x3E8 + i);
        
        if (i == 0) {
            biomeLayer = make_shared<AddIslandLayer>(seed, biomeLayer, 3);
        }
        
        if (i == 1 || zoomLevel == 1) {
            biomeLayer = make_shared<ShoreLayer>(seed, biomeLayer, 0x3E8);
        }
    }

    biomeLayer = make_shared<SmoothLayer>(seed, biomeLayer, 0x3E8);

    
    shared_ptr<Layer> mixed = make_shared<RiverMixerLayer>(seed, biomeLayer, riverLayerFinal, 0x64); 
    shared_ptr<Layer> voronoi = make_shared<VoronoiZoom>(seed, mixed, 0xA); 

    mixed->init(seed);
    voronoi->init(seed);

    LayerArray result(3, false);
    result[0] = mixed;    
    result[1] = voronoi;  
    result[2] = mixed;    
    return result;
}

	Layer::Layer(int64_t seedMixup)
	{
		parent = nullptr;

		this->seedMixup = seedMixup;
		this->seedMixup *= this->seedMixup * 6364136223846793005l + 1442695040888963407l;
		this->seedMixup += seedMixup;
		this->seedMixup *= this->seedMixup * 6364136223846793005l + 1442695040888963407l;
		this->seedMixup += seedMixup;
		this->seedMixup *= this->seedMixup * 6364136223846793005l + 1442695040888963407l;
		this->seedMixup += seedMixup;
	}



	void Layer::initRandom(int64_t x, int64_t y)
	{
		rval = seed;
		rval *= rval * 6364136223846793005l + 1442695040888963407l;
		rval += x;
		rval *= rval * 6364136223846793005l + 1442695040888963407l;
		rval += y;
		rval *= rval * 6364136223846793005l + 1442695040888963407l;
		rval += x;
		rval *= rval * 6364136223846793005l + 1442695040888963407l;
		rval += y;
	}

	int Layer::nextRandom(int max)
	{
	#ifdef __PSVITA__
		// AP - 64bit mods are very slow on Vita. Replaced with a divide/mult for general case and a fast divide library for specific numbers
		// todo - this can sometimes yield a different number to the original. There's a strange bug sometimes with Vita where if the line
		// "result = (int) ((rval >> 24) % max);" is done twice in a row 'result' will not be the same. Need to speak to Sony about that
		// Also need to compare level against a different platform using the same seed
		int result;
		long long temp = rval;
		temp >>= 24;
		if( max == 2 )
		{
			result = temp-(temp/fast_d2)*2;
		}
		else if( max == 3 )
		{
			result = temp-(temp/fast_d3)*3;
		}
		else if( max == 4 )
		{
			result = temp-(temp/fast_d4)*4;
		}
		else if( max == 5 )
		{
			result = temp-(temp/fast_d5)*5;
		}
		else if( max == 6 )
		{
			result = temp-(temp/fast_d6)*6;
		}
		else if( max == 7 )
		{
			result = temp-(temp/fast_d7)*7;
		}
		else if( max == 10 )
		{
			result = temp-(temp/fast_d10)*10;
		}
		else
		{
			result = temp-(temp/max)*max;
		}
	#else

		int result = static_cast<int>((rval >> 24) % max);
	#endif

		if (result < 0) result += max;
		rval *= rval * 6364136223846793005l + 1442695040888963407l;
		rval += seed;
		return result;
	}

void Layer::init(int64_t seed) 
{
    this->seed = seed;
    if (parent != nullptr) parent->init(seed);
    this->seed *= this->seed * 6364136223846793005LL + 1442695040888963407LL;
    this->seed += seedMixup;
    this->seed *= this->seed * 6364136223846793005LL + 1442695040888963407LL;
    this->seed += seedMixup;
    this->seed *= this->seed * 6364136223846793005LL + 1442695040888963407LL;
    this->seed += seedMixup;
}