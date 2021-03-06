using System.Collections.Generic;
using STRINGS;
using TUNING;
using UnityEngine;
using CREATURES = STRINGS.CREATURES;

namespace PalmeraTree
{
	public class PalmeraTreeConfig : IEntityConfig
	{
		public const string ID = "PalmeraTreePlant";
		public const string SEED_ID = "PalmeraTreeSeed";

		public static LocString Name = (LocString)UI.FormatAsLink("Palmera Tree", ID.ToUpper());
		public static LocString Desc = (LocString)("A large, chlorine-dwelling " + UI.FormatAsLink("Plant", "PLANTS") + " that can be grown in farm buildings.\n\nPalmeras grow inedible buds that emit toxic hydrogen gas.");
		public static LocString DomesticatedDesc = (LocString)("A large, chlorine-dwelling " + UI.FormatAsLink("Plant", "PLANTS") + " that grows inedible buds which emit toxic hydrogen gas.");

		public static LocString SeedName = (LocString)UI.FormatAsLink("Palmera Tree Seed", ID.ToUpper());
		public static LocString SeedDesc = (LocString)"The " + UI.FormatAsLink("Seed", "PLANTS") + " of a " + Name + ".";


		public GameObject CreatePrefab()
		{
			GameObject placedEntity = EntityTemplates.CreatePlacedEntity(ID, Name, Desc, 1f,
				Assets.GetAnim("palmeratree_kanim"), "idle_loop", Grid.SceneLayer.BuildingFront, 2, 3, DECOR.BONUS.TIER2, defaultTemperature: 350f);
			EntityTemplates.ExtendEntityToBasicPlant(placedEntity, 258.15f, 323.15f, 363.15f, 373.15f, new SimHashes[] { SimHashes.ChlorineGas }, true, 0.0f, 0.15f, PalmeraBerryConfig.ID);

			placedEntity.AddOrGet<PalmeraTree>();
			var consumer = placedEntity.AddOrGet<ElementConsumer>();
			consumer.elementToConsume = SimHashes.ChlorineGas;
			consumer.consumptionRate = 0.001f;

			var emitter = placedEntity.AddOrGet<ElementEmitter>();
			emitter.outputElement = new ElementConverter.OutputElement(0.001f, SimHashes.Hydrogen, outputElementOffsety: 2f);
			emitter.maxPressure = 1.8f;

			EntityTemplates.CreateAndRegisterPreviewForPlant(
				EntityTemplates.CreateAndRegisterSeedForPlant(placedEntity, SeedProducer.ProductionType.Harvest, SEED_ID,
					SeedName, SeedDesc, Assets.GetAnim("seed_palmeratree_kanim"), "object", 0, new List<Tag> { Utils.CropSeed2TileWide },
					SingleEntityReceptacle.ReceptacleDirection.Top, new Tag(), 6, CREATURES.SPECIES.JUNGLEGASPLANT.DOMESTICATEDDESC,
					EntityTemplates.CollisionShape.CIRCLE, 0.33f, 0.33f, null, string.Empty), "PalmeraTree_preview", Assets.GetAnim("palmeratree_kanim"), "idle_wilt_loop", 2, 3);

			SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_harvest", NOISE_POLLUTION.CREATURES.TIER3);
			SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_harvest", NOISE_POLLUTION.CREATURES.TIER3);

			return placedEntity;
		}

		public void OnPrefabInit(GameObject inst)
		{
		}

		public void OnSpawn(GameObject inst)
		{
		}
	}
}
