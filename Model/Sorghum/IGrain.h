#ifndef IGrainH
#define IGrainH

#include "PlantComponents.h"
#include "Utilities.h"

namespace Sorghum {
//------------------------------------------------------------------------------------------------
class IGrain 
   {
   public:
    IGrain() { } ;
    virtual ~IGrain() {};

   virtual void  process(void) = 0;

   // nitrogen
   virtual void  RetranslocateN(double N) = 0;

   // biomass
   virtual double partitionDM(double dltDM) = 0;
   virtual double grainDMDifferential(void) = 0;
   virtual void  dmRetrans(double dltDm) = 0;
   virtual void  Harvest(void) = 0;

   virtual double calcPRetransDemand(void) = 0;

   virtual void  Summary(void) = 0;
   };
}
#endif
