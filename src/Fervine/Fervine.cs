﻿using KSerialization;
using STRINGS;
using UnityEngine;

namespace Fervine
{
	public class Fervine : StateMachineComponent<Fervine.StatesInstance>
	{
		[MyCmpGet]
		private Light2D _lightSource;

		[SerializeField]
		private float _openEnergyThreshold;
		[SerializeField]
		private float _minTemperature;
		[SerializeField]
		private float _kjConsumptionRate;
		[SerializeField]
		private float _lightKjConsumptionRate;
		[SerializeField]
		private Vector2I _minCheckOffset;
		[SerializeField]
		private Vector2I _maxCheckOffset;
		[Serialize]
		private float _kjConsumed;

		protected override void OnSpawn()
		{
			base.OnSpawn();
			smi.Get<KBatchedAnimController>().randomiseLoopedOffset = true;
			_lightSource.enabled = false;

			_openEnergyThreshold = 5;
			_minTemperature = 293.15f;
			_kjConsumptionRate = 0.25f;
			_lightKjConsumptionRate = 0.2f;

			_minCheckOffset = new Vector2I(-2, -2);
			_maxCheckOffset = new Vector2I(2, 2);

			smi.StartSM();
		}

		protected void DestroySelf(object callbackParam)
		{
			CreatureHelpers.DeselectCreature(gameObject);
			Util.KDestroyGameObject(gameObject);
		}

		public Notification CreateDeathNotification()
		{
			return new Notification(CREATURES.STATUSITEMS.PLANTDEATH.NOTIFICATION, NotificationType.Bad, HashedString.Invalid, (notificationList, data) =>
					 CREATURES.STATUSITEMS.PLANTDEATH.NOTIFICATION_TOOLTIP + notificationList.ReduceMessages(false), "/t• " + gameObject.GetProperName());
		}

		public class StatesInstance : GameStateMachine<States, StatesInstance, Fervine, object>.GameInstance
		{
			public StatesInstance(Fervine master)
				: base(master)
			{
			}
		}

		public class States : GameStateMachine<States, StatesInstance, Fervine>
		{
			public AliveStates Alive;
			public State Dead;

			public override void InitializeStates(out BaseState defaultState)
			{
				serializable = true;
				defaultState = Alive;

				string plantname = CREATURES.STATUSITEMS.DEAD.NAME;
				string tooltip = CREATURES.STATUSITEMS.DEAD.TOOLTIP;
				StatusItemCategory main = Db.Get().StatusItemCategories.Main;

				Dead.ToggleStatusItem(plantname, tooltip, string.Empty, StatusItem.IconType.Info, 0, false, OverlayModes.None.ID, 0, null,
						null, main)
					.Enter(smi =>
					{
						if (!UprootedMonitor.IsObjectUprooted(masterTarget.Get(smi)))
						{
							smi.master.gameObject.AddOrGet<Notifier>().Add(smi.master.CreateDeathNotification(), string.Empty);
						}

						GameUtil.KInstantiate(Assets.GetPrefab(EffectConfigs.PlantDeathId), smi.master.transform.GetPosition(),
							Grid.SceneLayer.FXFront).SetActive(true);
						smi.master.Trigger((int)GameHashes.Died);
						smi.master.GetComponent<KBatchedAnimController>().StopAndClear();
						Destroy(smi.master.GetComponent<KBatchedAnimController>());
						smi.Schedule(0.5f, smi.master.DestroySelf);
					});

				Alive
					.InitializeStates(masterTarget, Dead)
					.DefaultState(Alive.Closed);

				Alive.Closed
					.PlayAnim("close")
					.Update("closed", (smi, dt) =>
					{
						AbsorbHeat(smi, dt);

						if (smi.master._kjConsumed > smi.master._openEnergyThreshold)
						{
							smi.GoTo(Alive.Open);
						}

					}, UpdateRate.SIM_1000ms);

				Alive.Open.PlayAnim("open", KAnim.PlayMode.Once)
					.Enter(smi => smi.master._lightSource.enabled = true)
					.Update("open", (smi, dt) =>
					{
						AbsorbHeat(smi, dt);

						smi.master._kjConsumed -= smi.master._lightKjConsumptionRate;

						if (smi.master._kjConsumed < smi.master._lightKjConsumptionRate)
						{
							smi.GoTo(Alive.Closed);
						}
					}, UpdateRate.SIM_1000ms)
					.Exit(smi => smi.master._lightSource.enabled = false);
			}

			private static void AbsorbHeat(StatesInstance smi, float dt)
			{
				float num1 = smi.master._kjConsumptionRate * dt;
				Vector2I vector2I = smi.master._maxCheckOffset - smi.master._minCheckOffset + 1;
				int num2 = vector2I.x * vector2I.y;
				float num3 = num1 / num2;
				Grid.PosToXY(smi.master.transform.position, out var x1, out var y1);
				for (int y2 = smi.master._minCheckOffset.y; y2 <= smi.master._maxCheckOffset.y; ++y2)
				{
					for (int x2 = smi.master._minCheckOffset.x; x2 <= smi.master._maxCheckOffset.x; ++x2)
					{
						int cell = Grid.XYToCell(x1 + x2, y1 + y2);
						if (Grid.IsValidCell(cell) && Grid.Temperature[cell] > (double)smi.master._minTemperature)
						{
							smi.master._kjConsumed += num3;
							SimMessages.ModifyEnergy(cell, -num3, 3000f, SimMessages.EnergySourceID.HeatBulb);
						}
					}
				}
			}

			public class AliveStates : PlantAliveSubState
			{
				public State Open;
				public State Closed;
			}
		}
	}
}