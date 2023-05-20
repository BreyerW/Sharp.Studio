using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Studio.View
{
    public class MainView : ImGuiView
    {

        public override void OnGUI()
        {
            //ImGui.SetNextWindowViewport(platform.MainViewport.ID);
            ImGui.SetNextWindowSize(new Vector2(1280, 720), ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.Begin("Sharp Studio", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.DockNodeHost | ImGuiWindowFlags.NoDocking);
            ImGui.PopStyleVar(3);

            var size = ImGui.GetWindowViewport().Size;

            ImGui.BeginMenuBar();
            ImGui.MenuItem("New");
            ImGui.EndMenuBar();
            var id = ImGui.GetID("_main_dockspace");Debug.WriteLine(ImGui.IsWindowDocked());
            ImGui.DockSpace(id, new Vector2(size.X, size.Y - ImGui.GetFrameHeight() * 3));
            
            ImGui.End();
        }
    }
}
