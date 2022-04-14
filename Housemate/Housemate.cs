﻿using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Housemate
{
    public class Housemate : IDalamudPlugin
    {
        private const string CommandName = "/housemate";

        private DalamudPluginInterface _pi;
        private CommandManager _commandManager;
        private HousemateUI _ui;
        private Configuration _configuration;
        public string Name => "Housemate";

        public Housemate(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] SigScanner sigScanner)
        {
            _pi = pluginInterface;
            _commandManager = commandManager;

            _configuration = _pi.GetPluginConfig() as Configuration ?? new Configuration();
            _configuration.Initialize(_pi);
            _ui = new HousemateUI(_configuration, objectTable, clientState, gameGui);

            commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = $"Display the Housemate configuration interface.\n" +
                              $"Toggle the Housemate object overlay with '{CommandName} toggle'"
            });

            HousingData.Init(dataManager);
            HousingMemory.Init(sigScanner);

            _pi.UiBuilder.Draw += DrawUI;
            _pi.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            _ui.Dispose();
            _commandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            if (args == "toggle")
            {
                _configuration.Render = !_configuration.Render;
                return;
            }
            
            _ui.Visible = !_ui.Visible;
        }
        
        private void DrawConfigUI()
        {
            _ui.Visible = !_ui.Visible;
        }
        
        private void DrawUI()
        {
            _ui.Draw();
        }
    }
}