namespace Archneter.Cli.Commands;

public static class UiTemplate
{
    public const string Html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Archneter Dashboard</title>
    <style>
        :root {
            --bg: #0f1115; --surface: #1e2128; --surface-hover: #2a2d35;
            --primary: #6366f1; --primary-hover: #4f46e5;
            --text: #e2e8f0; --text-muted: #94a3b8;
            --border: #334155; --success: #22c55e;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI', system-ui, sans-serif; }
        body { background-color: var(--bg); color: var(--text); padding: 2rem; }
        .container { max-width: 1200px; margin: 0 auto; display: grid; grid-template-columns: 400px 1fr; gap: 2rem; }
        
        /* Header */
        header { margin-bottom: 2rem; text-align: center; grid-column: 1 / -1; }
        h1 { font-size: 2.5rem; background: linear-gradient(to right, #6366f1, #a855f7); -webkit-background-clip: text; color: transparent; }
        
        /* Cards */
        .panel { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 1.5rem; }
        .panel h2 { margin-bottom: 1rem; font-size: 1.25rem; color: #fff; }
        
        /* Forms */
        .form-group { margin-bottom: 1rem; }
        label { display: block; margin-bottom: 0.5rem; font-weight: 500; font-size: 0.9rem; color: var(--text-muted); }
        input[type=text], select { width: 100%; padding: 0.75rem; border-radius: 8px; border: 1px solid var(--border); background: var(--bg); color: var(--text); font-size: 1rem; }
        input[type=text]:focus, select:focus { outline: none; border-color: var(--primary); }
        
        /* Arch Grid */
        .arch-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; margin-bottom: 1rem; }
        .arch-card { border: 1px solid var(--border); padding: 1rem; border-radius: 8px; cursor: pointer; text-align: center; transition: all 0.2s; }
        .arch-card:hover { border-color: var(--primary); background: var(--surface-hover); }
        .arch-card.active { border-color: var(--primary); background: rgba(99, 102, 241, 0.1); box-shadow: 0 0 0 1px var(--primary); }
        
        /* Checkbox */
        .toggle { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; margin-bottom: 0.5rem; }
        .toggle input { accent-color: var(--primary); width: 1.2rem; height: 1.2rem; }
        
        /* Buttons */
        .btn-group { display: flex; gap: 1rem; margin-top: 1.5rem; }
        button { flex: 1; padding: 0.8rem; border-radius: 8px; font-weight: bold; font-size: 1rem; cursor: pointer; border: none; transition: 0.2s; }
        .btn-preview { background: var(--surface-hover); color: var(--text); border: 1px solid var(--border); }
        .btn-preview:hover { background: #333; }
        .btn-generate { background: var(--primary); color: white; }
        .btn-generate:hover { background: var(--primary-hover); }
        
        /* Terminal */
        .terminal { background: #000; border-radius: 12px; padding: 1rem; border: 1px solid var(--border); font-family: 'Fira Code', monospace; font-size: 0.85rem; height: 600px; overflow-y: auto; white-space: pre-wrap; color: #a9b7c6; }
        .terminal::-webkit-scrollbar { width: 8px; }
        .terminal::-webkit-scrollbar-thumb { background: #444; border-radius: 4px; }
    </style>
</head>
<body>
    <header>
        <h1>Archneter UI</h1>
        <p style=""color: var(--text-muted); margin-top: 0.5rem;"">Interactive Architecture Scaffolding Dashboard</p>
    </header>

    <div class=""container"">
        <!-- Configuration Panel -->
        <div class=""panel"">
            <h2>Project Setup</h2>
            <div class=""form-group"">
                <label>Project Name</label>
                <input type=""text"" id=""projectName"" placeholder=""e.g. MyAwesomeApp"" value=""DemoApp"">
            </div>

            <div class=""form-group"">
                <label>Target Directory (Absolute Path)</label>
                <input type=""text"" id=""targetDirectory"" placeholder=""/home/user/Desktop/Projects"" value="""">
            </div>
            
            <label>Architecture</label>
            <div class=""arch-grid"" id=""archGrid"">
                <div class=""arch-card active"" data-arch=""clean"">Clean<br>Arch</div>
                <div class=""arch-card"" data-arch=""verticalslice"">Vertical<br>Slice</div>
                <div class=""arch-card"" data-arch=""modularmonolith"">Modular<br>Monolith</div>
                <div class=""arch-card"" data-arch=""microservices"">Micro-<br>services</div>
                <div class=""arch-card"" data-arch=""n-tier"">N-Tier</div>
            </div>

            <div class=""form-group"" id=""servicesGroup"" style=""display: none;"">
                <label>Services/Modules (comma separated)</label>
                <input type=""text"" id=""servicesList"" placeholder=""Catalog, Orders, Identity"">
            </div>

            <div class=""form-group"">
                <label class=""toggle"">
                    <input type=""checkbox"" id=""includeTests""> Include Unit & Integration Tests
                </label>
            </div>

            <div class=""btn-group"">
                <button class=""btn-preview"" onclick=""executeCommand(true)"">Preview Tree</button>
                <button class=""btn-generate"" onclick=""executeCommand(false)"" id=""btnGen"">Generate</button>
                <button class=""btn-generate"" onclick=""openFolder()"" id=""btnOpen"" style=""display: none; background: var(--success);"">Open Folder</button>
            </div>
        </div>

        <!-- Terminal Panel -->
        <div class=""panel"" style=""display: flex; flex-direction: column;"">
            <h2>Console Output</h2>
            <div class=""terminal"" id=""terminal"">Welcome to Archneter CLI. Waiting for commands...
</div>
        </div>
    </div>

    <script>
        let selectedArch = 'clean';
        let lastTargetDir = '';
        
        // Select Architecture
        document.querySelectorAll('.arch-card').forEach(card => {
            card.addEventListener('click', () => {
                document.querySelectorAll('.arch-card').forEach(c => c.classList.remove('active'));
                card.classList.add('active');
                selectedArch = card.getAttribute('data-arch');
                
                const srvGroup = document.getElementById('servicesGroup');
                if(selectedArch === 'microservices' || selectedArch === 'modularmonolith' || selectedArch === 'verticalslice') {
                    srvGroup.style.display = 'block';
                    if(selectedArch === 'verticalslice') srvGroup.querySelector('label').innerText = 'Features (comma separated)';
                    else if(selectedArch === 'modularmonolith') srvGroup.querySelector('label').innerText = 'Modules (comma separated)';
                    else srvGroup.querySelector('label').innerText = 'Services (comma separated)';
                } else {
                    srvGroup.style.display = 'none';
                }
            });
        });

        async function openFolder() {
            if(!lastTargetDir) return;
            await fetch('/api/open', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ TargetDirectory: lastTargetDir })
            });
        }

        async function executeCommand(isDryRun) {
            const projName = document.getElementById('projectName').value || 'MyProject';
            const targetDir = document.getElementById('targetDirectory').value;
            const includeTests = document.getElementById('includeTests').checked;
            const services = document.getElementById('servicesList').value;
            const term = document.getElementById('terminal');
            const btn = document.getElementById('btnGen');
            const btnOpen = document.getElementById('btnOpen');
            
            let args = `new ${projName} --arch ${selectedArch}`;
            if(includeTests) args += ` --tests true`;
            if(isDryRun) args += ` --dry-run`;
            
            if(services) {
                if(selectedArch === 'microservices') args += ` --services ${services}`;
                if(selectedArch === 'modularmonolith') args += ` --modules ${services}`;
                if(selectedArch === 'verticalslice') args += ` --features ${services}`;
            }

            args += ` --force`;

            term.innerHTML += `\n\n> archneter ${args}\nExecuting...\n`;
            term.scrollTop = term.scrollHeight;
            if(!isDryRun) { 
                btn.innerHTML = 'Generating...'; 
                btn.disabled = true; 
                btnOpen.style.display = 'none';
                btn.style.display = 'block';
            }

            try {
                const response = await fetch('/api/execute', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ CommandArgs: args, TargetDirectory: targetDir })
                });
                
                const data = await response.json();
                lastTargetDir = data.actualTarget + '/' + projName;
                
                // Format CLI output with colors (basic parsing)
                let formatted = data.output
                    .replace(/\[dry-run\]/g, '<span style=""color: #f59e0b;"">[dry-run]</span>');
                
                term.innerHTML += formatted;
                
                if(!isDryRun) {
                    btn.style.display = 'none';
                    btnOpen.style.display = 'block';
                }
            } catch (err) {
                term.innerHTML += `\n<span style=""color: #ef4444"">Error: ${err.message}</span>`;
            } finally {
                term.scrollTop = term.scrollHeight;
                if(!isDryRun) { 
                    btn.innerHTML = 'Generate'; 
                    btn.disabled = false; 
                }
            }
        }
    </script>
</body>
</html>";
}
