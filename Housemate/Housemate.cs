using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace Housemate
{
    public class Housemate : IDalamudPlugin
    {
        private const string CommandName = "/housemate";

        private DalamudPluginInterface _pi;
        private HousemateUI _ui;
        private Configuration _configuration;
        public string Name => "Housemate";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pi = pluginInterface;

            _configuration = _pi.GetPluginConfig() as Configuration ?? new Configuration();
            _configuration.Initialize(_pi);
            _ui = new HousemateUI(_configuration, _pi);

            _pi.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "/housemate will open the Housemate window."
            });

            HousingData.Init(_pi);
            HousingMemory.Init(_pi);

            _pi.UiBuilder.OnBuildUi += DrawUI;
            _pi.UiBuilder.OnOpenConfigUi += (_, _) => DrawConfigUI();
        }

        public void Dispose()
        {
            _ui.Dispose();

            _pi.CommandManager.RemoveHandler(CommandName);
            _pi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            _ui.Visible = true;
        }

        private void DrawUI()
        {
            _ui.Draw();
        }

        private void DrawConfigUI()
        {
            _ui.SettingsVisible = true;
        }
    }
}