# DevVault - Test Plan & Verification

Complete testing guide to verify all functionality works correctly.

## Quick Start

```bash
cd e:\claude-code\projects\dev-vault\devvault
npm start
```

App launches with DevTools open on port 9000.

---

## Phase 1: Core Functionality Testing

### Test 1: Create a Snippet
**Steps:**
1. Click "➕ New Snippet" button in sidebar
2. Enter title: `"Hello World Function"`
3. Select language: `"javascript"`
4. Paste code:
   ```javascript
   function helloWorld() {
     console.log("Hello, World!");
   }
   ```
5. Add tags: Type `"basic"` and press Enter, then `"javascript"`
6. Click "➕ Create"

**Expected:**
- Snippet saves successfully
- Message shows "Update" button instead of "Create"
- Returns to list view
- Snippet appears in grid with title, language tag, and code preview

**Verify:**
- [ ] Snippet appears in list
- [ ] Language badge shows "javascript"
- [ ] Code preview shows first 100 chars
- [ ] Tags appear at bottom ("basic", "javascript")

---

### Test 2: Search Functionality
**Steps:**
1. Make sure you have at least one snippet created
2. Type "hello" in the search box
3. Observe results update in real-time

**Expected:**
- Results filter as you type
- Shows matching snippets
- Shows result count in sidebar

**Verify:**
- [ ] Search filters work
- [ ] Results show only matching snippets
- [ ] Clear "X" button appears when searching

---

### Test 3: Syntax Highlighting
**Steps:**
1. Create another snippet with Python code:
   ```python
   def fibonacci(n):
       if n <= 1:
           return n
       return fibonacci(n-1) + fibonacci(n-2)
   ```
2. Select language "python"
3. Click snippet to view

**Expected:**
- Code preview shows proper syntax highlighting
- Keywords (def, if, return) colored differently
- String colors different from keywords
- Comments visible

**Verify:**
- [ ] Syntax highlighting appears for Python
- [ ] Colors are visible in preview pane
- [ ] Code is readable with highlighting

---

### Test 4: Copy to Clipboard
**Steps:**
1. Open a snippet in editor
2. Click "📋 Copy" button
3. Open text editor elsewhere (Notepad, VS Code)
4. Paste (Ctrl+V)

**Expected:**
- Success toast appears
- Code contents paste into text editor exactly as written

**Verify:**
- [ ] Toast message appears
- [ ] Pasted content matches original code exactly
- [ ] All characters (special chars, indentation) preserved

---

### Test 5: Tag Filtering
**Steps:**
1. Create 3 snippets with different tags:
   - Snippet A: tags = ["react", "hooks"]
   - Snippet B: tags = ["react", "components"]
   - Snippet C: tags = ["vue", "components"]
2. Click "▶ Advanced Filters"
3. Check "react"
4. Check "hooks"

**Expected:**
- Only Snippet A shows (has both "react" AND "hooks")
- B and C hidden
- Shows: "2 tags selected"

**Verify:**
- [ ] AND logic works (shows only snippets with ALL selected tags)
- [ ] Tag count updates
- [ ] Filter badges show selected tags
- [ ] "Clear All" button appears

---

### Test 6: Edit & Update Snippet
**Steps:**
1. Click on a snippet in the list
2. Editor view opens
3. Change title to: `"Updated Title"`
4. Modify code by adding a comment
5. Update a tag
6. Click "💾 Update"

**Expected:**
- Changes persist
- Snippet reappears in list with new title
- Click to re-open shows updated content

**Verify:**
- [ ] Title updated
- [ ] Code changes saved
- [ ] Tags updated
- [ ] All changes visible after re-opening

---

### Test 7: Delete Snippet
**Steps:**
1. Open a snippet in editor
2. Click "🗑️ Delete" button
3. Confirm delete dialog
4. Return to list

**Expected:**
- Confirmation dialog appears
- Upon confirm, snippet removed from list
- Snippet no longer appears in database

**Verify:**
- [ ] Delete confirmation shows
- [ ] Snippet removed from list
- [ ] Snippet gone even after app restart

---

## Phase 2: Settings Testing

### Test 8: Settings Dialog
**Steps:**
1. Click "⚙️" button in header (top right)
2. Settings dialog opens

**Expected:**
- Modal appears with settings
- Can see theme selector (Light/Dark)
- Font size slider appears
- Cloud sync checkbox present
- About section shows version

**Verify:**
- [ ] Settings dialog modal appears
- [ ] All sections visible
- [ ] Close button works (✕)
- [ ] Overlay click closes dialog

---

### Test 9: Theme Toggle
**Steps:**
1. Open Settings (⚙️)
2. Click "🌙 Dark" button (if not already selected)
3. Click "Save Settings"
4. Observe app colors

**Expected:**
- Theme changes (or stays same if already dark)
- Button highlights indicating selection
- Save persists selection

**Verify:**
- [ ] Theme button highlights when clicked
- [ ] Can toggle between Light and Dark
- [ ] Save Settings button works

---

## Phase 3: Export/Import Testing

**Note**: This requires clicking the export/import button (📤📥) in the header

### Test 10: Export Snippets
**Steps:**
1. Create 2-3 snippets
2. Click "📤📥" button in header
3. Click "Export" tab (should be already open)
4. Click "📥 Download All Snippets"

**Expected:**
- Success message appears: "✓ Export successful! File downloaded."
- JSON file downloads (check Downloads folder)
- File named `devvault-export-YYYY-MM-DD.json`

**Verify:**
- [ ] Export button works
- [ ] File downloads
- [ ] File contains valid JSON
- [ ] File contains all snippet data

**Manual Verification:**
Open downloaded JSON file - should show:
```json
{
  "version": "1.0",
  "exportedAt": 1645...
  "snippets": [
    {
      "id": "uuid...",
      "title": "...",
      "content": "...",
      "language": "...",
      "tags": [...],
      "createdAt": ...,
      "updatedAt": ...
    }
  ]
}
```

---

### Test 11: Import Snippets
**Steps:**
1. Open Export/Import dialog (📤📥)
2. Click "Import" tab
3. Export a snippet first (Test 10)
4. Delete all snippets manually
5. In Import tab, click "📤 Choose JSON File"
6. Select the exported JSON file
7. Wait for import to complete

**Expected:**
- Success message shows: "✓ Import successful! Imported: X, Skipped: Y"
- Deleted snippets reappear in list
- All data (tags, code, metadata) restored

**Verify:**
- [ ] Import dialog opens file picker
- [ ] Can select JSON file
- [ ] Import completes successfully
- [ ] Snippets reappear with correct data
- [ ] Tags preserved
- [ ] Code content unchanged

---

## Phase 4: Data Persistence Testing

### Test 12: App Restart Persistence
**Steps:**
1. Create 3 snippets with different tags
2. Quit the app completely (Ctrl+Q or close window)
3. Reopen app (`npm start`)
4. Wait for app to load

**Expected:**
- All 3 snippets reappear in list
- Tags intact
- Code content unchanged
- Snippet count in sidebar correct

**Verify:**
- [ ] All snippets persist across restarts
- [ ] Database file still exists at `~/.config/devvault/devvault.db`
- [ ] No data loss

---

## Phase 5: Edge Cases & Error Handling

### Test 13: Empty State
**Steps:**
1. Delete all snippets
2. App should show empty state

**Expected:**
- Message appears: "No snippets found"
- Helptext: "Create one to get started →"
- New Snippet button still works

**Verify:**
- [ ] Empty state displays correctly
- [ ] Can create snippet from empty state

---

### Test 14: Large Code Snippets
**Steps:**
1. Create snippet with large code (500+ lines)
2. Preview should show truncated first 2 lines
3. Click to edit - full code loads

**Expected:**
- Preview truncates properly
- Full code displays in editor
- No performance issues
- Copy works for large snippets

**Verify:**
- [ ] Large snippets handle correctly
- [ ] Preview truncates at 100 chars
- [ ] Full code displays in editor
- [ ] No freezing/lag

---

### Test 15: Special Characters
**Steps:**
1. Create snippet with special chars:
   ```
   émojis 🎉, symbols !@#$%, unicode ñ, tabs	  newlines
   ```
2. Save and re-open

**Expected:**
- All special characters preserved
- Copy/paste works with special chars
- No corruption

**Verify:**
- [ ] Special chars preserved
- [ ] Copy works for special chars
- [ ] Preview shows them correctly

---

## Complete Manual Test Checklist

Print this and check off each test:

```
PHASE 1: CORE FUNCTIONALITY
[ ] Test 1: Create Snippet
[ ] Test 2: Search Functionality
[ ] Test 3: Syntax Highlighting
[ ] Test 4: Copy to Clipboard
[ ] Test 5: Tag Filtering
[ ] Test 6: Edit & Update
[ ] Test 7: Delete Snippet

PHASE 2: SETTINGS
[ ] Test 8: Settings Dialog
[ ] Test 9: Theme Toggle

PHASE 3: EXPORT/IMPORT
[ ] Test 10: Export Snippets
[ ] Test 11: Import Snippets

PHASE 4: PERSISTENCE
[ ] Test 12: App Restart

PHASE 5: EDGE CASES
[ ] Test 13: Empty State
[ ] Test 14: Large Code
[ ] Test 15: Special Characters
```

---

## Troubleshooting

### App won't start
- Check Node version: `node --version` (16+ required)
- Try: `npm install` and `npm start` again
- Delete `.webpack` folder and rebuild

### Database issues
- Database file: `~/.config/devvault/devvault.db` (Linux/macOS)
- Delete file to reset (WARNING: deletes all snippets)

### Syntax highlighting not working
- Refresh DevTools: Ctrl+Shift+R
- Type `rs` in terminal to restart main process

### CSS not loading
- Restart dev server: Kill and run `npm start` again
- Check browser console for CSS load errors

---

## Performance Targets

- **App startup**: < 2 seconds
- **Create snippet**: < 500ms
- **Search filter**: Real-time, < 100ms
- **Export 100 snippets**: < 1 second
- **Import 100 snippets**: < 2 seconds

---

## Browser DevTools

When app runs, DevTools console shows:
- IPC errors
- React component errors
- Database errors

Main process logs appear in **terminal**.

---

## Success Criteria

✅ All 15 tests pass
✅ No console errors
✅ No data loss on restart
✅ All UI interactions work
✅ Export/Import JSON valid
✅ Performance acceptable
✅ Special characters handled
