# 📝 DevVault

A **local-first code snippet manager** with advanced features for developers. Store, search, and organize your code snippets offline with syntax highlighting, tag filtering, and export/import capabilities.

## ✨ Features

- ✅ **Local-first storage** - All data stored locally with SQLite
- ✅ **Syntax highlighting** - 20+ languages supported via highlight.js
- ✅ **Full-text search** - Search by title and content
- ✅ **Tag filtering** - Organize snippets with tags (AND logic)
- ✅ **Dark mode** - Built-in light/dark theme toggle
- ✅ **Copy to clipboard** - One-click copy with keyboard shortcut
- ✅ **Export/Import** - Backup and restore snippets as JSON
- ✅ **Keyboard shortcuts** - Ctrl+N (new), Ctrl+F (search), Ctrl+S (save), Escape (back)
- ✅ **Responsive UI** - Works on various screen sizes
- ✅ **Zero ads** - Privacy-focused, no tracking
- ✅ **Toast notifications** - Real-time feedback for actions
- ✅ **Colored language borders** - Visual language identification

## 🚀 Quick Start

### Prerequisites
- Node.js 14+ and npm

### Installation

```bash
# Navigate to project directory
cd dev-vault/devvault

# Install dependencies
npm install

# Start development server
npm start
```

The app will launch in an Electron window with hot-reload support for development.

### Building for Distribution

```bash
# Create distributable packages for your platform
npm run make

# Output will be in the out/ directory
```

## 📋 Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` / `Cmd+N` | Create new snippet |
| `Ctrl+F` / `Cmd+F` | Focus search bar |
| `Ctrl+S` / `Cmd+S` | Save current snippet |
| `Escape` | Go back / Close modal |

## 🎨 Themes

Toggle between **Light** and **Dark** themes in Settings (⚙️). Your preference is automatically saved.

- **Light**: Clean white interface for daylight
- **Dark**: Eye-friendly dark theme for extended coding sessions

## 📦 Export & Import

### Export Snippets
- Click 📤 button in header
- Download all snippets (or filtered selection) as JSON
- Perfect for backups

### Import Snippets
- Click 📥 button in header
- Select a previously exported JSON file
- Choose conflict handling:
  - **Skip**: Keep existing snippets
  - **Merge**: Combine tags from both versions
  - **Overwrite**: Replace with imported version

## 🏗 Tech Stack

| Component | Technology |
|-----------|------------|
| **Desktop Framework** | Electron 41.2.0 |
| **UI Framework** | React 19.2.5 |
| **Language** | TypeScript 5.0.2 |
| **Database** | SQLite (better-sqlite3) |
| **Build Tool** | Webpack 5 + Electron Forge |
| **Syntax Highlighting** | highlight.js 11.11.1 |
| **State Persistence** | electron-store |
| **Styling** | CSS Modules + CSS Variables |

## 📁 Project Structure

```
src/
├── main/                    # Electron main process
│   ├── index.ts             # App initialization & IPC setup
│   ├── database.ts          # SQLite CRUD & queries
│   ├── ipc.ts               # IPC event handlers
│   └── preload.ts           # Secure context bridge
├── renderer/                # React UI (Electron renderer)
│   ├── App.tsx              # Main app component
│   ├── App.css              # Global styles
│   ├── index.tsx            # React root
│   ├── index.html           # HTML template
│   ├── index.css            # Root styles with CSS vars
│   └── components/          # React components
│       ├── SnippetList.tsx  # Grid view
│       ├── SnippetEditor.tsx # Editor with preview
│       ├── SearchBar.tsx    # Search & filters
│       ├── TagFilter.tsx    # Tag selector
│       ├── Settings.tsx     # Settings modal
│       ├── ConfirmModal.tsx # Confirmation dialog
│       ├── ExportImport.tsx # Export/Import interface
│       ├── Toast.tsx        # Notifications
│       └── *.css            # Component styles
└── shared/
    └── types.ts             # TypeScript interfaces

database/
└── schema.sql               # SQLite table definitions

```

## 🔐 Security & Privacy

- **No cloud required** - All data stays on your machine
- **Context isolation** - Renderer process sandboxed
- **Secure IPC** - Context bridge restricts main process access
- **No tracking** - No telemetry or external requests
- **Open source** - Inspect the code yourself

## 🚴 Development

### Start development server
```bash
npm start
```
- Webpack dev server runs on localhost:9000
- Hot reload enabled for quick iteration
- DevTools opened by default

### TypeScript checking
```bash
npx tsc --noEmit
```

### Build for distribution
```bash
npm run make
```
Supported platforms:
- Windows (.msi, .zip)
- macOS (.dmg)
- Linux (.deb, .rpm)

## 📝 License

MIT License - Use freely for personal and commercial purposes

## 🙋 FAQ

**Q: Where is my data stored?**
A: In SQLite database at `~/.config/devvault/devvault.db` (Linux/Mac) or `%APPDATA%\devvault\devvault.db` (Windows)

**Q: Can I sync across devices?**
A: Use Export/Import to backup and move data between machines

**Q: What if the database gets corrupted?**
A: Delete the .db file to start fresh (data will be lost; always keep backups)

**Q: How do I contribute?**
A: Submit issues on GitHub or fork and send pull requests

---

**Made with ❤️ for developers who love local-first tools**
│       ├── SnippetEditor.tsx # Create/edit editor
│       ├── SearchBar.tsx     # Search with filters
│       └── TagFilter.tsx     # Tag filtering widget
└── shared/
    └── types.ts              # TypeScript interfaces

database/
└── schema.sql                # SQLite schema

webpack.*.config.js           # Webpack configuration
forge.config.js               # Electron Forge configuration
tsconfig.json                 # TypeScript configuration
```

## 🚀 Getting Started

### Prerequisites
- Node.js 16+ and npm 8+
- Windows, macOS, or Linux

### Installation

```bash
cd devvault
npm install
```

### Development

Start the development server with hot reload:

```bash
npm start
```

This launches:
- Electron app window
- Webpack dev server on http://localhost:9000
- DevTools automatically open for debugging

### Build for Production

```bash
npm run make
```

This creates platform-specific installers in the `out/` directory.

## 📚 Database Schema

### Tables
- **snippets**: Main snippet storage (id, title, content, language, timestamps)
- **tags**: Tag definitions (id, name)
- **snippet_tags**: Many-to-many relationship

### Key Features
- Full-text search on title and content
- Tag-based filtering with AND logic (snippets must have ALL selected tags)
- Indexed for fast queries on large collections

## 💻 Usage

### Create a Snippet
1. Click **"➕ New Snippet"** button in the sidebar
2. Enter title and paste code
3. Select programming language (auto-detected in preview)
4. Add tags (start typing, press Enter to add)
5. Click **"➕ Create"** to save

### Search & Filter
1. Type in **search box** to find by title/content
2. Click **"▶ Advanced Filters"** to expand:
   - Select multiple tags (AND logic)
   - Filter by language
3. Results update in real-time

### Edit a Snippet
1. Click on a snippet in the list
2. Modify title, content, language, or tags
3. Click **"💾 Update"** to save
4. Click **"📋 Copy"** to copy code to clipboard
5. Click **"🗑️ Delete"** to remove (with confirmation)

### Clear Filters
- Click **"Clear All"** button in active filter badges to reset search

## 🔍 Database Queries

The app supports:
- **Text search**: `LIKE` queries on title and content (case-insensitive)
- **Tag filtering**: Uses multi-tag AND logic (snippet must have all selected tags)
- **Language filtering**: Exact match on selected language
- **Combined**: All filters work together

## 🔐 Security

- **Secure IPC**: Context isolation between main/renderer processes
- **Sandbox**: Renderer process runs in sandbox mode
- **Local Storage**: All data stored locally (no cloud by default)
- **JSON Export**: Shareable but unencrypted; keep private data safe

## 🎨 Styling

All styling is pure CSS (no frameworks) for minimal dependencies:
- Color scheme: Purple accent (#667eea) on light background
- Responsive breakpoints at 768px and 1024px
- Smooth transitions and hover effects
- Syntax highlighting via highlight.js themes

## 🧪 Testing

### Manual Testing Checklist
- [ ] Create snippet with title, code, language, tags
- [ ] Verify snippet appears in list with preview
- [ ] Search by text - results update in real-time
- [ ] Filter by tag - shows only matching snippets
- [ ] Filter by language - shows only that language
- [ ] Edit snippet - changes persist after save
- [ ] Copy button - code goes to clipboard
- [ ] Delete snippet - removes from list
- [ ] Advanced filters expand/collapse correctly
- [ ] Clear filters button resets all searches
- [ ] Refresh app - data persists (SQLite)

## 📝 Development Notes

### Adding a New IPC Handler
1. Add handler in `src/main/ipc.ts` using `ipcMain.handle()`
2. Add API method in `src/main/preload.ts`
3. Call from renderer via `window.api`

### Adding a Database Query
1. Add SQL in `src/main/database.ts`
2. Export function for use in IPC handlers
3. Add IPC handler to expose to renderer

### Adding a React Component
1. Create `.tsx` file in `src/renderer/components/`
2. Import in parent component
3. Add corresponding `.css` file for styling
4. Use TypeScript types from `src/shared/types.ts`

## 🐛 Troubleshooting

### App won't start
- Ensure `npm install` completed successfully
- Check Node.js version: `node --version` (16+ required)
- Delete `.webpack` folder and try again: `rm -rf .webpack && npm start`

### Database errors
- Database file is stored in `~/.config/devvault/devvault.db` (Linux/macOS) or `%APPDATA%/devvault/devvault.db` (Windows)
- Delete to reset (will lose all snippets)

### CSS not loading
- Webpack dev server may need restart: Type `rs` in terminal and press Enter
- Hard refresh in DevTools: Ctrl+Shift+R (Windows/Linux) or Cmd+Shift+R (macOS)

## 📋 Roadmap

- **v1.1**: Export/import JSON, bulk operations
- **v1.2**: Cloud sync (Dropbox/OneDrive), backup & restore
- **v1.3**: Settings panel, theme customization, shortcuts
- **v1.4**: Snippet sharing (share links), public gallery
- **v2.0**: Web version, mobile companion app

## 📄 License

MIT

## 🤝 Contributing

Contributions welcome! Please follow the existing code style and add tests for new features.

## 📞 Support

For issues or feature requests, please open a GitHub issue.

## Verification checklist

Before publishing changes, run the documented build and test commands, verify database migrations against a disposable database, and do not include local credentials or generated application data.
