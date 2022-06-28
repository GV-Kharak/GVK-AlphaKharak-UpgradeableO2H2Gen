using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace MaO2
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenGenerator), false, "MA_O2")]
    public class OxyGen : MyGameLogicComponent
    {
        private IMyGasGenerator thisGenerator;
        private IMyTerminalBlock thisTerminalBlock;
        private MyCubeBlock thisCubeBlock;

        private const string Power = "PowerEfficiency";
        private const string Yield = "Effectiveness";
        private const string Speed = "Productivity";
        private const float BasePowerConsumptionMultiplier = 1f;
        private const float BaseProductionCapacityMultiplier = 1f;

        private float baseOxyMaxOutput;
        private float baseHydroMaxOutput;

        private readonly MyDefinitionId oxyDef = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");

        private readonly MyDefinitionId hydroDef =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Hydrogen");

        private MyResourceSinkComponent MySink => ((MyResourceSinkComponent)thisTerminalBlock.ResourceSink);

        private MyResourceSourceComponent MyResource => thisCubeBlock.Components.Get<MyResourceSourceComponent>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;

            thisCubeBlock = (MyCubeBlock)Entity;
            thisTerminalBlock = (IMyTerminalBlock)Entity;
            thisGenerator = (IMyGasGenerator)Entity;
            thisGenerator.OnUpgradeValuesChanged += OnUpgradeValuesChanged;
            thisGenerator.AppendingCustomInfo += AppendingCustomInfo;
            MySink.RequiredInputChanged += OnRequiredInputChanged;
            thisGenerator.OnClose += OnClose;
            thisGenerator.AddUpgradeValue(Power, 1f);
            thisGenerator.AddUpgradeValue(Yield, 1f);
            thisGenerator.AddUpgradeValue(Speed, 1f);

            var def = (MyOxygenGeneratorDefinition)thisGenerator.SlimBlock.BlockDefinition;

            foreach (var resource in def.ProducedGases)
            {
                if (resource.Id == oxyDef)
                {
                    baseOxyMaxOutput = MyResource.DefinedOutputByType(resource.Id);
                }

                if (resource.Id == hydroDef)
                {
                    baseHydroMaxOutput = MyResource.DefinedOutputByType(resource.Id);
                }
            }
        }

        private void OnRequiredInputChanged(MyDefinitionId unused1, MyResourceSinkComponent unused2, float unused3,
            float unused4)
        {
            thisTerminalBlock.RefreshCustomInfo();
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder value)
        {
            if (block != thisTerminalBlock)
            {
                return;
            }

            UpdateInfo(value);
            UpdateTerminal();
        }

        private void UpdateTerminal()
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (thisCubeBlock.IDModule != null)
            {
                ownerId = thisCubeBlock.IDModule.Owner;
                shareMode = thisCubeBlock.IDModule.ShareMode;
            }
            else
            {
                var block = thisCubeBlock as IMyTerminalBlock;
                if (block == null)
                {
                    return;
                }

                block.ShowOnHUD = !block.ShowOnHUD;
                block.ShowOnHUD = !block.ShowOnHUD;
                return;
            }

            thisCubeBlock.ChangeOwner(ownerId,
                shareMode == MyOwnershipShareModeEnum.None
                    ? MyOwnershipShareModeEnum.Faction
                    : MyOwnershipShareModeEnum.None);
            thisCubeBlock.ChangeOwner(ownerId, shareMode);
        }

        private void OnClose(IMyEntity closedEntity)
        {
            thisGenerator.OnUpgradeValuesChanged -= OnUpgradeValuesChanged;
            thisGenerator.AppendingCustomInfo -= AppendingCustomInfo;
            MySink.RequiredInputChanged -= OnRequiredInputChanged;
            thisGenerator.OnClose -= OnClose;
        }

        private void UpdateInfo(StringBuilder detailedInfo)
        {
            //detailedInfo.Clear();
            detailedInfo.Append("\n");
            detailedInfo.Append("Actual Max Required: ");
            MyValueFormatter.AppendWorkInBestUnit(
                MySink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), detailedInfo);
            detailedInfo.Append("\n");
            detailedInfo.Append("Current Power Use: ");
            MyValueFormatter.AppendWorkInBestUnit(
                thisGenerator.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId),
                detailedInfo);
            detailedInfo.AppendFormat("\n\n");
            detailedInfo.Append("Power Efficiency: ");
            detailedInfo.Append(((1f / thisGenerator.PowerConsumptionMultiplier) * 100f).ToString(" 0"));
            detailedInfo.Append("%\n");
            detailedInfo.Append("Resource Efficiency: ");
            detailedInfo.Append(((1f / thisGenerator.ProductionCapacityMultiplier) * 100.0).ToString(" 0"));
            detailedInfo.Append("%\n");
            detailedInfo.Append("Speed Multiplier: ");
            detailedInfo.Append(((thisGenerator.UpgradeValues[Speed]) * 100.0).ToString(" 0"));
            detailedInfo.Append("%\n");
        }

        private void OnUpgradeValuesChanged()
        {
            float power;
            float speed;
            float yield;

            if (!thisGenerator.UpgradeValues.TryGetValue(Power, out power))
            {
                power = 1;
            }

            if (!thisGenerator.UpgradeValues.TryGetValue(Yield, out yield))
            {
                yield = 1;
            }

            if (!thisGenerator.UpgradeValues.TryGetValue(Speed, out speed))
            {
                speed = 1;
            }

            thisGenerator.PowerConsumptionMultiplier =
                (BasePowerConsumptionMultiplier / power) * speed * yield; // Power Efficiency
            thisGenerator.ProductionCapacityMultiplier = (BaseProductionCapacityMultiplier / (yield >= 1 ? yield : 1) *
                                                          (speed > 1 ? (speed * 0.15f) + 1 : speed)); // Yield
            MyResource.SetMaxOutputByType(oxyDef, baseOxyMaxOutput * speed);
            MyResource.SetMaxOutputByType(hydroDef, baseHydroMaxOutput * speed);
        }
    }
}