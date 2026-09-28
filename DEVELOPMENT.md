# Development Guide - DevVault

Quick reference for developers working on DevVault enhancements.

## 🚀 Quick Start

```bash
cd devvault
npm install
npm start
```

App opens in Electron window with DevTools. Type `rs` in terminal to restart main process during development.

## 📐 Architecture Patterns

### Adding a New Feature

#### 1. Create a Database Function (if needed)

File: `src/main/database.ts`

```typescript
export function myNewQuery(params: MyParams): Result[] {
  const db = getDatabase();
  const stmt = db.prepare('SELECT * FROM snippets WHERE ...');
  return stmt.all(...);
}
```

#### 2. Create an IPC Handler

File: `src/main/ipc.ts`

```typescript
ipcMain.handle('feature:action', async (event, input) => {
  try {
    const result = myNewQuery(input);
    return { success: true, data: result };
  } catch (error) {
    return { success: false, error: (error as any).message };
  }
});
```

#### 3. Expose in Preload

File: `src/main/preload.ts`

```typescript
contextBridge.exposeInMainWorld('api', {
  // ... existing API
  feature: {
    action: (input: MyInput) => ipcRenderer.invoke('feature:action', input),
  },
});
// Update Window interface type definitions
```

#### 4. Use in React Component

File: `src/renderer/components/MyComponent.tsx`

```typescript
const handleAction = async () => {
  const result = await window.api.feature.action(params);
  if (result.success) {
    // Handle success
  }
};
```

## 🏗️ File Organization

### Main Process (`src/main/`)
- **index.ts** - Electron app lifecycle, window creation
- **database.ts** - All SQLite operations
- **ipc.ts** - IPC channel handlers
- **preload.ts** - Secure bridge to renderer

### Renderer (`src/renderer/`)
- **index.tsx** - React root mount
- **index.html** - HTML entry
- **App.tsx** - Main layout & state management
- **components/** - React components

### Shared (`src/shared/`)
- **types.ts** - TypeScript interfaces used everywhere

## 🔄 Data Flow

```
React Component (renderer)
    ↓ (calls window.api)
Preload Bridge (main/preload.ts)
    ↓ (ipcRenderer.invoke)
IPC Handler (main/ipc.ts)
    ↓ (calls database function)
Database (main/database.ts)
    ↓ (SQLite query)
SQLite File (~/.config/devvault/devvault.db)
```

## 🎨 Component Development

### Creating a New Component

1. **File structure**:
   ```
   src/renderer/components/
   ├── MyComponent.tsx      (logic)
   └── MyComponent.css      (styles)
   ```

2. **Template**:
   ```typescript
   import React, { useState } from 'react';
   import './MyComponent.css';

   interface MyComponentProps {
     title: string;
     onAction?: () => void;
   }

   export default function MyComponent({ title, onAction }: MyComponentProps) {
     const [state, setState] = useState('');

     return (
       <div className="my-component">
         <h2>{title}</h2>
         {/* JSX content */}
       </div>
     );
   }
   ```

3. **Import in parent**:
   ```typescript
   import MyComponent from './components/MyComponent';
   ```

## 🗄️ Database Operations

### Query Pattern

```typescript
// Read
const snippets = getAllSnippets();
const snippet = getSnippetById(id);
const results = searchSnippets({ query: 'foo', tags: ['react'] });

// Create
const newSnippet = createSnippet({
  title: 'My Snippet',
  content: '...',
  language: 'javascript',
  tags: ['react', 'hooks'],
});

// Update
const updated = updateSnippet(id, {
  title: 'Updated',
  tags: ['react', 'context-api'],
});

// Delete
const deleted = deleteSnippet(id);
```

### Adding a Tag Filter

Modify `searchSnippets()` in `src/main/database.ts`:

```typescript
export function searchSnippets(filters: SearchFilters): Snippet[] {
  let query = `...`;
  const params: any[] = [];

  // Your new filter
  if (filters.yourNewFilter) {
    query += ` AND your_column = ?`;
    params.push(filters.yourNewFilter);
  }

  // ... rest of query
}
```

## 🎯 Common Tasks

### Add a new language to syntax highlighting

In `src/renderer/components/SnippetEditor.tsx`, update the `LANGUAGES` array:

```typescript
const LANGUAGES = [
  'javascript',
  'typescript',
  'python',
  // ... add here
  'your-language',
];
```

### Change the color scheme

Edit CSS variables in `src/renderer/index.css`:

```css
:root {
  --primary: #667eea;        /* Change to your color */
  --primary-dark: #5568d3;
  /* ... other colors */
}
```

### Add a keyboard shortcut

In `src/renderer/App.tsx`:

```typescript
useEffect(() => {
  const handleKeyPress = (e: KeyboardEvent) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault();
      // Handle Ctrl+S
    }
  };

  window.addEventListener('keydown', handleKeyPress);
  return () => window.removeEventListener('keydown', handleKeyPress);
}, []);
```

## 🧪 Testing During Development

### Manual Test Flow

1. **Create a snippet**
   - Click "New Snippet"
   - Fill in title, content, language, tags
   - Click "Create"

2. **Verify it persists**
   - Quit app (Ctrl+Q)
   - Restart: `npm start`
   - Snippet should still be there

3. **Test search**
   - Type in search box
   - Results should filter in real-time

4. **Test editing**
   - Click a snippet
   - Change content/tags
   - Click "Update"
   - Go back to list - should show updated data

## 🐛 Debugging

### Enable detailed logging

Add to `src/main/index.ts`:

```typescript
if (process.env.NODE_ENV === 'development') {
  mainWindow.webContents.openDevTools();
}
```

### View SQLite database

Install a SQLite browser (e.g., DB Browser for SQLite) and open:
- Linux/macOS: `~/.config/devvault/devvault.db`
- Windows: `%APPDATA%/devvault/devvault.db`

### Hot reload not working

Type `rs` in terminal to manually restart main process, or:

```bash
# Rebuild everything
rm -rf .webpack
npm start
```

## 📦 Building

### For development (with hot reload):
```bash
npm start
```

### For production:
```bash
npm run make
```

Creates installers in `out/` directory (platform-specific).

## 🚀 Next Phase Checklist

- [ ] Phase 2: Advanced tag management
  - [ ] Tag creation UI
  - [ ] Tag rename/delete
  - [ ] Tag suggestions
  
- [ ] Phase 3: Organization
  - [ ] Folder/collection UI
  - [ ] Move snippets between folders
  - [ ] Bulk operations
  
- [ ] Phase 4: Export/Import
  - [ ] Export dialog with filters
  - [ ] Import dialog with conflict resolution
  - [ ] Batch import
  
- [ ] Phase 5: Cloud Sync
  - [ ] Settings panel
  - [ ] Folder watchdog implementation
  - [ ] Sync status indicator
  
- [ ] Phase 6: Backup
  - [ ] Backup dialog
  - [ ] Restore dialog
  - [ ] Version history

## 📝 Code Style

- Use TypeScript for type safety
- Props interfaces ending with `Props`
- Keep components focused (single responsibility)
- Use CSS classes instead of inline styles
- Prefix CSS classes with component name

## 🔗 Resources

- [Electron Docs](https://www.electronjs.org/docs)
- [React Docs](https://react.dev)
- [SQLite Docs](https://www.sqlite.org/docs.html)
- [better-sqlite3](https://github.com/WiseLibs/better-sqlite3)
- [highlight.js](https://highlightjs.org/)

## 💡 Tips

- Use `console.log()` in renderer - logs appear in DevTools console
- Use `console.log()` in main process - logs appear in terminal
- SQLite file is created automatically on first run
- IPC is async by design - always await `window.api` calls
- React components auto-reload on file changes (hot reload)
