using GameCreator.Runtime.Common;

namespace GameCreator.Runtime.SaveablePrefabs
{
    public class SaveablePrefabsSettings : AssetRepository<SaveablePrefabsRepository>
    {
        public override IIcon Icon => new IconComputer(ColorTheme.Type.TextLight);
        public override string Name => "Saveable Prefabs";
    }
}
