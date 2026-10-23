# StageDeck

StageDeck is a Windows desktop window shelf inspired by macOS Stage Manager. A slim shelf stays at the left edge of the desktop, shows live previews of assigned windows, and automatically adds minimized unassigned windows. Click a preview to restore its window. Named stages let you group windows and switch between workspaces.

## Run

Open `StageDeck/StageDeck.csproj` in Visual Studio 2022 with the .NET desktop development workload, or run `dotnet run --project StageDeck/StageDeck.csproj` on Windows with the .NET 8 Windows Desktop targeting pack installed.

## Daily use

1. Launch StageDeck. The shelf remains visible at the left edge.
2. Minimized windows appear automatically. Click a preview to restore the window.
3. Choose **Add current window** to assign open windows to the selected stage.
4. Use **Stages** to create or select a stage. **Switch stage** brings that stage forward and shelves the other assigned windows.
5. Press **Ctrl+Alt+Space** to toggle the shelf. The notification-area menu can restore windows or exit StageDeck.

Stage names and assignments live in `%LOCALAPPDATA%\StageDeck\stages.json`. Previews use Windows Desktop Window Manager thumbnails. Protected or elevated windows may not provide previews, and Windows may block foreground activation in some situations.
