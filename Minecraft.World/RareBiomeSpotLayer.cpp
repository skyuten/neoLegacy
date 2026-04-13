#include "stdafx.h"
#include "RareBiomeSpotLayer.h"
#include "IntCache.h"

RareBiomeSpotLayer::RareBiomeSpotLayer(int64_t seed, std::shared_ptr<Layer> parent, int64_t seedMixup) : Layer(seedMixup)
{
	this->parent = parent;
}

intArray RareBiomeSpotLayer::getArea(int xo, int yo, int w, int h)
{
	
	return parent->getArea(xo, yo, w, h);
}
