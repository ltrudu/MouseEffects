# Guide du Workflow de Release avec GitHub Actions

Un guide complet et accessible aux débutants pour automatiser votre processus de release avec GitHub Actions.

---

## Table des Matières

1. [Introduction](#1-introduction)
2. [Prérequis](#2-prérequis)
3. [Comprendre GitHub Actions](#3-comprendre-github-actions)
4. [Le Workflow de Release Expliqué](#4-le-workflow-de-release-expliqué)
5. [Guide de Configuration Étape par Étape](#5-guide-de-configuration-étape-par-étape)
6. [Outil Git Publish Workflow (Interface Python)](#6-outil-git-publish-workflow-interface-python)
7. [Guide d'Utilisation](#7-guide-dutilisation)
8. [Dépannage](#8-dépannage)
9. [Conseils de Personnalisation](#9-conseils-de-personnalisation)

---

## 1. Introduction

### Qu'est-ce que le CI/CD ?

**CI/CD** signifie **Intégration Continue / Déploiement Continu** :

- **Intégration Continue (CI)** : Compiler et tester automatiquement votre code à chaque push
- **Déploiement Continu (CD)** : Déployer/publier automatiquement votre application après des builds réussis

### Qu'est-ce que GitHub Actions ?

GitHub Actions est la plateforme d'automatisation intégrée de GitHub qui vous permet de :

- **Automatiser des workflows** directement dans votre dépôt
- **Compiler, tester et déployer** votre code automatiquement
- **Déclencher des actions** basées sur des événements (push, pull request, tags, planification, etc.)
- **Utiliser des actions préconçues** du marketplace ou créer les vôtres

### Pourquoi Utiliser des Releases Automatisées ?

| Releases Manuelles | Releases Automatisées |
|-------------------|----------------------|
| Sujettes aux erreurs (oubli d'étapes) | Cohérentes à chaque fois |
| Chronophages | Rapides et efficaces |
| Difficiles à reproduire | Builds reproductibles |
| Incohérences de versions | Versionnage automatique |
| Nécessitent du temps développeur | Configurez et oubliez |

### Comment Fonctionne Notre Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKFLOW DE RELEASE                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   1. Le développeur crée un tag (ex: v1.0.38)               │
│                    ↓                                         │
│   2. Push du tag vers GitHub                                 │
│                    ↓                                         │
│   3. Le workflow GitHub Actions se déclenche                 │
│                    ↓                                         │
│   4. Le code est récupéré sur une VM fraîche                │
│                    ↓                                         │
│   5. Les dépendances sont restaurées                         │
│                    ↓                                         │
│   6. L'application est compilée et publiée                   │
│                    ↓                                         │
│   7. L'installateur est créé (Velopack)                      │
│                    ↓                                         │
│   8. Une Release GitHub est créée avec les artefacts         │
│                    ↓                                         │
│   9. Les utilisateurs peuvent télécharger la nouvelle version│
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Prérequis

Avant de commencer, assurez-vous d'avoir :

- **Un compte GitHub** avec un dépôt
- **Git** installé sur votre ordinateur
- **Connaissances de base en Git** : clone, commit, push, pull, tags
- **Python 3.x** (pour l'outil GUI - optionnel mais recommandé)
- **Les outils de build de votre projet** (ex: .NET SDK, Node.js, etc.)

### Introduction aux Tags Git

Les tags Git sont comme des marque-pages pour des commits spécifiques. Ils sont couramment utilisés pour les releases :

```bash
# Créer un tag
git tag v1.0.0

# Créer un tag annoté avec un message
git tag -a v1.0.0 -m "Version 1.0.0"

# Pousser un tag spécifique vers le remote
git push origin v1.0.0

# Pousser tous les tags
git push --tags

# Lister tous les tags
git tag -l

# Supprimer un tag local
git tag -d v1.0.0

# Supprimer un tag distant
git push origin --delete v1.0.0
```

---

## 3. Comprendre GitHub Actions

### Concepts Clés

#### Workflow (Flux de travail)
Un processus automatisé configurable défini dans un fichier YAML. Les workflows sont situés dans le répertoire `.github/workflows/`.

#### Job (Tâche)
Un ensemble d'étapes qui s'exécutent sur le même runner (machine virtuelle). Les jobs peuvent s'exécuter en parallèle ou séquentiellement.

#### Step (Étape)
Une tâche individuelle au sein d'un job. Les étapes peuvent exécuter des commandes ou utiliser des actions.

#### Action
Une unité de code réutilisable. Peut provenir du GitHub Marketplace ou être personnalisée.

#### Runner (Exécuteur)
Un serveur qui exécute vos workflows. GitHub fournit des runners hébergés (Ubuntu, Windows, macOS).

#### Event/Trigger (Événement/Déclencheur)
Ce qui démarre un workflow (push, pull request, tag, planification, manuel, etc.).

### Bases de la Syntaxe YAML

```yaml
# Les commentaires commencent par #

# Paires clé-valeur
name: Mon Workflow
version: 1.0

# Listes (tableaux)
steps:
  - element1
  - element2
  - element3

# Structure imbriquée
job:
  name: Build
  runs-on: ubuntu-latest
  steps:
    - name: Checkout
      uses: actions/checkout@v4

# Chaînes multi-lignes
run: |
  echo "Ligne 1"
  echo "Ligne 2"
  echo "Ligne 3"

# Variables d'environnement
env:
  MA_VARIABLE: "valeur"

# Utilisation des variables
run: echo ${{ env.MA_VARIABLE }}
```

### Déclencheurs Courants

```yaml
on:
  # À chaque push sur la branche main
  push:
    branches: [main]

  # Sur les pull requests vers main
  pull_request:
    branches: [main]

  # Sur le push d'un tag (pour les releases)
  push:
    tags:
      - 'v*'  # Correspond à v1.0.0, v2.1.3, etc.

  # Déclenchement manuel depuis l'interface GitHub
  workflow_dispatch:
    inputs:
      version:
        description: 'Numéro de version'
        required: true

  # Planifié (syntaxe cron)
  schedule:
    - cron: '0 0 * * *'  # Tous les jours à minuit
```

---

## 4. Le Workflow de Release Expliqué

Voici notre workflow de release complet avec des commentaires détaillés :

```yaml
# ============================================================
# WORKFLOW : Build et Release
# ============================================================
# Ce workflow compile et publie automatiquement l'application
# lorsqu'un tag de version (v*) est poussé vers le dépôt.
# ============================================================

name: Build and Release

# ============================================================
# DÉCLENCHEURS
# ============================================================
on:
  # Déclencheur 1 : Quand un tag commençant par 'v' est poussé
  # Exemples : v1.0.0, v2.1.3, v1.0.0-beta
  push:
    tags:
      - 'v*'

  # Déclencheur 2 : Déclenchement manuel depuis l'interface GitHub Actions
  # Utile pour tester ou créer des releases sans tags
  workflow_dispatch:
    inputs:
      version:
        description: 'Numéro de version (ex: 1.0.3)'
        required: true
        type: string

# ============================================================
# VARIABLES D'ENVIRONNEMENT
# ============================================================
# Disponibles pour tous les jobs et étapes
env:
  DOTNET_VERSION: '8.0.x'
  PROJECT_PATH: 'src/MouseEffects.App/MouseEffects.App.csproj'
  TARGET_FRAMEWORK: 'net8.0-windows10.0.19041.0'

# ============================================================
# PERMISSIONS
# ============================================================
# Nécessaires pour créer des releases et uploader des assets
permissions:
  contents: write

# ============================================================
# JOBS (TÂCHES)
# ============================================================
jobs:
  build:
    # Exécuter sur Windows (requis pour les apps .NET Windows)
    runs-on: windows-latest

    steps:
      # ----------------------------------------------------------
      # ÉTAPE 1 : Récupérer le Code
      # ----------------------------------------------------------
      # Télécharge le code de votre dépôt sur le runner
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Historique complet pour le versionnage

      # ----------------------------------------------------------
      # ÉTAPE 2 : Configurer le SDK .NET
      # ----------------------------------------------------------
      # Installe la version spécifiée du SDK .NET
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      # ----------------------------------------------------------
      # ÉTAPE 3 : Déterminer la Version
      # ----------------------------------------------------------
      # Extrait la version du tag (v1.0.0 -> 1.0.0)
      # ou utilise l'entrée manuelle
      - name: Determine version
        id: version
        shell: pwsh
        run: |
          if ("${{ github.event.inputs.version }}" -ne "") {
            $version = "${{ github.event.inputs.version }}"
          } else {
            $version = "${{ github.ref_name }}".TrimStart('v')
          }
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
          echo "Version: $version"

      # ----------------------------------------------------------
      # ÉTAPE 4 : Restaurer les Dépendances
      # ----------------------------------------------------------
      # Télécharge les packages NuGet
      - name: Restore dependencies
        run: dotnet restore

      # ----------------------------------------------------------
      # ÉTAPE 5 : Compiler la Solution
      # ----------------------------------------------------------
      # Compile le code en mode Release
      - name: Build solution (x64)
        run: dotnet build --configuration Release --no-restore -p:Platform=x64

      # ----------------------------------------------------------
      # ÉTAPE 6 : Publier l'Application
      # ----------------------------------------------------------
      # Crée un déploiement autonome
      - name: Publish application (x64)
        run: |
          dotnet publish ${{ env.PROJECT_PATH }} `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output ./publish/win-x64 `
            -p:PublishSingleFile=false `
            -p:Version=${{ steps.version.outputs.VERSION }}

      # ----------------------------------------------------------
      # ÉTAPE 7 : Copier les Plugins
      # ----------------------------------------------------------
      # Copie les DLL des plugins dans le dossier de publication
      - name: Copy plugins (x64)
        shell: pwsh
        run: |
          $pluginsSource = "./src/MouseEffects.App/bin/x64/Release/${{ env.TARGET_FRAMEWORK }}/plugins"
          $pluginsDest = "./publish/win-x64/plugins"
          if (Test-Path $pluginsSource) {
            New-Item -ItemType Directory -Force -Path $pluginsDest | Out-Null
            Copy-Item -Path "$pluginsSource\*" -Destination $pluginsDest -Recurse -Force
            Write-Host "Plugins x64 copiés"
          }

      # ----------------------------------------------------------
      # ÉTAPE 8 : Installer Velopack CLI
      # ----------------------------------------------------------
      # Velopack crée des installateurs professionnels avec mise à jour auto
      - name: Install Velopack CLI
        run: dotnet tool install -g vpk

      # ----------------------------------------------------------
      # ÉTAPE 9 : Créer le Package d'Installation
      # ----------------------------------------------------------
      # Package l'application dans un installateur
      - name: Create Velopack release (x64)
        shell: pwsh
        run: |
          vpk pack `
            --packId MouseEffects `
            --packVersion ${{ steps.version.outputs.VERSION }} `
            --packDir ./publish/win-x64 `
            --mainExe MouseEffects.App.exe `
            --outputDir ./releases/x64 `
            --packTitle "MouseEffects" `
            --icon ./src/MouseEffects.App/MouseEffects.ico

      # ----------------------------------------------------------
      # ÉTAPE 10 : Renommer les Artefacts
      # ----------------------------------------------------------
      # Renomme les fichiers pour inclure l'info d'architecture
      - name: Rename artifacts
        shell: pwsh
        run: |
          Move-Item -Path "./releases/x64/MouseEffects-win-Setup.exe" `
                    -Destination "./releases/MouseEffects-x64-Setup.exe" -Force

          Get-ChildItem -Path "./releases/x64/*.nupkg" | ForEach-Object {
            $newName = $_.Name -replace "-full\.nupkg$", "-x64-full.nupkg"
            Move-Item -Path $_.FullName -Destination "./releases/$newName" -Force
          }

      # ----------------------------------------------------------
      # ÉTAPE 11 : Uploader les Artefacts
      # ----------------------------------------------------------
      # Sauvegarde les outputs de build pour téléchargement ultérieur
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: MouseEffects-${{ steps.version.outputs.VERSION }}
          path: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*.nupkg
            ./releases/RELEASES-*
          retention-days: 30

      # ----------------------------------------------------------
      # ÉTAPE 12 : Créer la Release GitHub (Déclencheur Tag)
      # ----------------------------------------------------------
      # Crée une release avec les liens de téléchargement
      - name: Create GitHub Release
        if: startsWith(github.ref, 'refs/tags/v')
        uses: softprops/action-gh-release@v2
        with:
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
          draft: false
          prerelease: ${{ contains(github.ref_name, '-') }}
          generate_release_notes: true
          files: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*-x64-full.nupkg
            ./releases/RELEASES-x64
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      # ----------------------------------------------------------
      # ÉTAPE 13 : Créer la Release GitHub (Déclencheur Manuel)
      # ----------------------------------------------------------
      # Pour les exécutions manuelles, crée une release brouillon
      - name: Create GitHub Release (manual trigger)
        if: github.event_name == 'workflow_dispatch'
        uses: softprops/action-gh-release@v2
        with:
          tag_name: v${{ steps.version.outputs.VERSION }}
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
          draft: true
          prerelease: false
          generate_release_notes: true
          files: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*-x64-full.nupkg
            ./releases/RELEASES-x64
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## 5. Guide de Configuration Étape par Étape

### Étape 1 : Créer le Répertoire du Workflow

Dans votre dépôt, créez la structure de répertoires suivante :

```
votre-depot/
├── .github/
│   └── workflows/
│       └── release.yml
├── src/
│   └── ... votre code ...
└── README.md
```

### Étape 2 : Créer le Fichier Workflow

Créez `.github/workflows/release.yml` avec le contenu de la Section 4 ci-dessus.

### Étape 3 : Configurer les Permissions du Dépôt

1. Allez sur votre dépôt sur GitHub
2. Cliquez sur **Settings** > **Actions** > **General**
3. Descendez jusqu'à **Workflow permissions**
4. Sélectionnez **Read and write permissions**
5. Cochez **Allow GitHub Actions to create and approve pull requests**
6. Cliquez sur **Save**

### Étape 4 : Pousser le Workflow

```bash
git add .github/workflows/release.yml
git commit -m "Ajout du workflow de release"
git push
```

### Étape 5 : Tester avec un Déclenchement Manuel

1. Allez dans l'onglet **Actions** de votre dépôt
2. Cliquez sur le workflow **Build and Release**
3. Cliquez sur **Run workflow**
4. Entrez un numéro de version (ex: `1.0.0-test`)
5. Cliquez sur **Run workflow**
6. Observez la progression !

### Étape 6 : Créer Votre Première Release

```bash
# Assurez-vous d'être sur la bonne branche
git checkout main
git pull

# Créer et pousser un tag
git tag v1.0.0
git push origin v1.0.0
```

Le workflow se déclenchera automatiquement et créera une release !

---

## 6. Outil Git Publish Workflow (Interface Python)

Pour faciliter encore plus les releases, nous avons créé un outil GUI Python qui :

- Récupère le dernier tag de version
- Incrémente automatiquement le numéro de version
- Crée et pousse les tags en un clic
- Peut supprimer les tags distants (pour re-déclencher les workflows)

### Le Code Python Complet

Sauvegardez ceci sous `git-publish-workflow.py` :

```python
#!/usr/bin/env python3
"""
Interface Graphique Git Publish Workflow

Ouvre une interface pour créer et pousser un tag de version
afin de déclencher le workflow GitHub Actions.
Exécute : git tag v[majeur].[mineur].[patch] && git push origin v[majeur].[mineur].[patch]
"""

import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext
import subprocess
import threading
import os


class GitPublishWorkflowApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Git Publish Workflow")
        self.root.geometry("600x500")
        self.root.resizable(True, True)

        # Récupérer le répertoire de travail actuel
        self.cwd = os.getcwd()

        # Configurer les poids de la grille pour le redimensionnement
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(2, weight=1)

        self._create_widgets()
        self._fetch_latest_tag()

    def _create_widgets(self):
        # Affichage du répertoire de travail
        dir_frame = ttk.LabelFrame(self.root, text="Répertoire de Travail", padding=10)
        dir_frame.grid(row=0, column=0, padx=10, pady=5, sticky="ew")
        dir_frame.columnconfigure(0, weight=1)

        self.dir_label = ttk.Label(dir_frame, text=self.cwd, wraplength=550)
        self.dir_label.grid(row=0, column=0, sticky="w")

        # Cadre de saisie de version
        version_frame = ttk.LabelFrame(self.root, text="Numéro de Version", padding=10)
        version_frame.grid(row=1, column=0, padx=10, pady=5, sticky="ew")

        # Champs de saisie de version
        ttk.Label(version_frame, text="Majeur:").grid(row=0, column=0, padx=5, pady=5)
        self.major_var = tk.StringVar(value="1")
        self.major_entry = ttk.Entry(version_frame, textvariable=self.major_var, width=8, justify="center")
        self.major_entry.grid(row=0, column=1, padx=5, pady=5)

        ttk.Label(version_frame, text=".").grid(row=0, column=2)

        ttk.Label(version_frame, text="Mineur:").grid(row=0, column=3, padx=5, pady=5)
        self.minor_var = tk.StringVar(value="0")
        self.minor_entry = ttk.Entry(version_frame, textvariable=self.minor_var, width=8, justify="center")
        self.minor_entry.grid(row=0, column=4, padx=5, pady=5)

        ttk.Label(version_frame, text=".").grid(row=0, column=5)

        ttk.Label(version_frame, text="Patch:").grid(row=0, column=6, padx=5, pady=5)
        self.patch_var = tk.StringVar(value="0")
        self.patch_entry = ttk.Entry(version_frame, textvariable=self.patch_var, width=8, justify="center")
        self.patch_entry.grid(row=0, column=7, padx=5, pady=5)

        # Aperçu de la version
        self.version_preview_var = tk.StringVar(value="v1.0.0")
        ttk.Label(version_frame, text="Tag:").grid(row=0, column=8, padx=(20, 5), pady=5)
        self.version_preview = ttk.Label(version_frame, textvariable=self.version_preview_var,
                                         font=("Consolas", 12, "bold"), foreground="blue")
        self.version_preview.grid(row=0, column=9, padx=5, pady=5)

        # Lier les changements d'entrée à la mise à jour de l'aperçu
        self.major_var.trace_add("write", self._update_preview)
        self.minor_var.trace_add("write", self._update_preview)
        self.patch_var.trace_add("write", self._update_preview)

        # Info du dernier tag
        self.latest_tag_var = tk.StringVar(value="Récupération du dernier tag...")
        ttk.Label(version_frame, textvariable=self.latest_tag_var, foreground="gray").grid(
            row=1, column=0, columnspan=10, sticky="w", pady=(5, 0))

        # Cadre des boutons
        button_frame = ttk.Frame(version_frame)
        button_frame.grid(row=2, column=0, columnspan=10, pady=10)

        self.publish_btn = ttk.Button(button_frame, text="Publier le Tag", command=self._publish_tag)
        self.publish_btn.pack(side="left", padx=5)

        self.delete_btn = ttk.Button(button_frame, text="Supprimer le Tag Distant", command=self._delete_remote_tag)
        self.delete_btn.pack(side="left", padx=5)

        self.refresh_btn = ttk.Button(button_frame, text="Rafraîchir les Tags", command=self._fetch_latest_tag)
        self.refresh_btn.pack(side="left", padx=5)

        # Cadre de sortie
        output_frame = ttk.LabelFrame(self.root, text="Sortie Git", padding=10)
        output_frame.grid(row=2, column=0, padx=10, pady=5, sticky="nsew")
        output_frame.columnconfigure(0, weight=1)
        output_frame.rowconfigure(0, weight=1)

        self.output_text = scrolledtext.ScrolledText(output_frame, wrap=tk.WORD,
                                                      font=("Consolas", 10), height=15)
        self.output_text.grid(row=0, column=0, sticky="nsew")

        # Configurer les tags de texte pour la coloration
        self.output_text.tag_configure("error", foreground="red")
        self.output_text.tag_configure("success", foreground="green")
        self.output_text.tag_configure("info", foreground="blue")
        self.output_text.tag_configure("command", foreground="purple", font=("Consolas", 10, "bold"))

        # Bouton effacer
        clear_btn = ttk.Button(output_frame, text="Effacer la Sortie", command=self._clear_output)
        clear_btn.grid(row=1, column=0, pady=(5, 0))

    def _update_preview(self, *args):
        """Met à jour le label d'aperçu de version."""
        try:
            major = self.major_var.get() or "0"
            minor = self.minor_var.get() or "0"
            patch = self.patch_var.get() or "0"
            self.version_preview_var.set(f"v{major}.{minor}.{patch}")
        except:
            pass

    def _get_version(self):
        """Récupère la chaîne de version depuis les champs de saisie."""
        major = self.major_var.get().strip()
        minor = self.minor_var.get().strip()
        patch = self.patch_var.get().strip()

        if not major or not minor or not patch:
            raise ValueError("Tous les champs de version doivent être remplis")

        try:
            int(major)
            int(minor)
            int(patch)
        except ValueError:
            raise ValueError("Les numéros de version doivent être des entiers")

        return f"v{major}.{minor}.{patch}"

    def _log(self, message, tag=None):
        """Affiche un message dans le widget de texte."""
        self.output_text.insert(tk.END, message + "\n", tag)
        self.output_text.see(tk.END)
        self.root.update_idletasks()

    def _clear_output(self):
        """Efface le widget de texte."""
        self.output_text.delete(1.0, tk.END)

    def _run_git_command(self, command, description):
        """Exécute une commande git et retourne le statut de succès."""
        self._log(f"\n> {' '.join(command)}", "command")
        self._log(f"  ({description})", "info")

        try:
            result = subprocess.run(
                command,
                capture_output=True,
                text=True,
                cwd=self.cwd
            )

            if result.stdout:
                self._log(result.stdout.strip())

            if result.stderr:
                if result.returncode == 0:
                    self._log(result.stderr.strip())
                else:
                    self._log(result.stderr.strip(), "error")

            if result.returncode == 0:
                self._log(f"  [OK] {description} terminé avec succès", "success")
                return True
            else:
                self._log(f"  [ÉCHEC] {description} a échoué avec le code {result.returncode}", "error")
                return False

        except Exception as e:
            self._log(f"  [ERREUR] {str(e)}", "error")
            return False

    def _fetch_latest_tag(self):
        """Récupère et affiche le dernier tag de version."""
        def fetch():
            try:
                result = subprocess.run(
                    ["git", "tag", "-l", "v*", "--sort=-v:refname"],
                    capture_output=True,
                    text=True,
                    cwd=self.cwd
                )

                if result.returncode == 0 and result.stdout.strip():
                    tags = result.stdout.strip().split("\n")
                    latest = tags[0] if tags else "Aucun tag de version trouvé"
                    self.root.after(0, lambda: self.latest_tag_var.set(f"Dernier tag : {latest}"))

                    # Parser et incrémenter la version patch pour suggestion
                    if latest.startswith("v"):
                        parts = latest[1:].split(".")
                        if len(parts) >= 3:
                            try:
                                self.root.after(0, lambda: self.major_var.set(parts[0]))
                                self.root.after(0, lambda: self.minor_var.set(parts[1]))
                                patch_num = parts[2].split("-")[0]
                                new_patch = str(int(patch_num) + 1)
                                self.root.after(0, lambda: self.patch_var.set(new_patch))
                            except:
                                pass
                else:
                    self.root.after(0, lambda: self.latest_tag_var.set("Aucun tag de version trouvé"))

            except Exception as e:
                self.root.after(0, lambda: self.latest_tag_var.set(f"Erreur : {str(e)}"))

        threading.Thread(target=fetch, daemon=True).start()

    def _publish_tag(self):
        """Crée et pousse le tag de version."""
        try:
            version = self._get_version()
        except ValueError as e:
            messagebox.showerror("Version Invalide", str(e))
            return

        if not messagebox.askyesno("Confirmer la Publication",
                                   f"Ceci va créer et pousser le tag : {version}\n\n"
                                   f"Cela déclenchera le workflow GitHub Actions.\n\n"
                                   f"Continuer ?"):
            return

        self.publish_btn.state(["disabled"])
        self.delete_btn.state(["disabled"])

        def publish():
            self._log(f"\n{'='*50}", "info")
            self._log(f"Publication du tag : {version}", "info")
            self._log(f"{'='*50}", "info")

            # Créer le tag
            success = self._run_git_command(
                ["git", "tag", version],
                f"Création du tag local {version}"
            )

            if not success:
                self._log("\nÉchec de la création du tag. Le tag existe peut-être déjà localement.", "error")
                self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
                self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
                return

            # Pousser le tag
            success = self._run_git_command(
                ["git", "push", "origin", version],
                f"Push du tag {version} vers origin"
            )

            if success:
                self._log(f"\n{'='*50}", "success")
                self._log(f"Tag {version} publié avec succès !", "success")
                self._log("Le workflow GitHub Actions devrait maintenant être déclenché.", "success")
                self._log(f"{'='*50}", "success")
            else:
                self._log("\nÉchec du push. Vérifiez votre connexion réseau et vos identifiants.", "error")

            self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
            self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
            self.root.after(0, self._fetch_latest_tag)

        threading.Thread(target=publish, daemon=True).start()

    def _delete_remote_tag(self):
        """Supprime le tag distant (utile pour re-déclencher le workflow)."""
        try:
            version = self._get_version()
        except ValueError as e:
            messagebox.showerror("Version Invalide", str(e))
            return

        if not messagebox.askyesno("Confirmer la Suppression",
                                   f"Ceci va supprimer le tag distant : {version}\n\n"
                                   f"Vous pourrez ensuite republier pour re-déclencher le workflow.\n\n"
                                   f"Continuer ?"):
            return

        self.publish_btn.state(["disabled"])
        self.delete_btn.state(["disabled"])

        def delete():
            self._log(f"\n{'='*50}", "info")
            self._log(f"Suppression du tag distant : {version}", "info")
            self._log(f"{'='*50}", "info")

            success = self._run_git_command(
                ["git", "push", "origin", "--delete", version],
                f"Suppression du tag distant {version}"
            )

            if success:
                self._run_git_command(
                    ["git", "tag", "-d", version],
                    f"Suppression du tag local {version}"
                )

                self._log(f"\n{'='*50}", "success")
                self._log(f"Tag {version} supprimé. Vous pouvez maintenant republier.", "success")
                self._log(f"{'='*50}", "success")
            else:
                self._log("\nÉchec de la suppression. Le tag n'existe peut-être pas sur le distant.", "error")

            self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
            self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
            self.root.after(0, self._fetch_latest_tag)

        threading.Thread(target=delete, daemon=True).start()


def main():
    root = tk.Tk()
    app = GitPublishWorkflowApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
```

### Comment Utiliser l'Outil

1. **Sauvegardez le script** à l'emplacement de votre choix
2. **Naviguez vers le dossier de votre projet** dans le terminal/invite de commandes
3. **Exécutez le script** : `python git-publish-workflow.py`
4. L'interface va :
   - Afficher votre répertoire actuel
   - Récupérer et afficher le dernier tag
   - Suggérer automatiquement la prochaine version (incrément patch)
5. **Cliquez sur "Publier le Tag"** pour créer et pousser le tag
6. Observez la sortie pour les messages de succès/erreur

### Créer Votre Propre Version

Pour adapter cet outil à vos besoins :

1. **Changer le titre de la fenêtre** : Modifiez `self.root.title("Git Publish Workflow")`
2. **Ajouter des boutons personnalisés** : Ajoutez plus de widgets `ttk.Button` dans `_create_widgets`
3. **Ajouter des vérifications pré-publication** : Ajoutez une validation dans `_publish_tag` avant de créer le tag
4. **Intégrer avec votre CI** : Modifiez `_run_git_command` pour exécuter des commandes supplémentaires

---

## 7. Guide d'Utilisation

### Déclencher une Release via Tag

**Option 1 : Ligne de Commande**
```bash
git tag v1.0.38
git push origin v1.0.38
```

**Option 2 : Utiliser l'Interface Python**
1. Exécutez `python git-publish-workflow.py`
2. Ajustez les numéros de version
3. Cliquez sur "Publier le Tag"

**Option 3 : Interface GitHub (Déclenchement Manuel)**
1. Allez dans l'onglet Actions
2. Sélectionnez "Build and Release"
3. Cliquez sur "Run workflow"
4. Entrez le numéro de version
5. Cliquez sur "Run workflow"

### Surveiller la Progression du Workflow

1. Allez dans l'onglet **Actions** de votre dépôt
2. Cliquez sur le workflow en cours d'exécution
3. Développez chaque étape pour voir les logs
4. Coche verte = succès, X rouge = échec

### Télécharger les Artefacts

**Depuis une Release :**
1. Allez sur la page **Releases**
2. Trouvez votre release
3. Téléchargez les assets sous "Assets"

**Depuis une Exécution de Workflow (avant la release) :**
1. Allez dans l'onglet **Actions**
2. Cliquez sur le workflow terminé
3. Descendez jusqu'à "Artifacts"
4. Téléchargez le ZIP de l'artefact

---

## 8. Dépannage

### Problèmes Courants et Solutions

#### "Le tag existe déjà"
```bash
# Supprimer le tag local
git tag -d v1.0.38

# Supprimer le tag distant
git push origin --delete v1.0.38

# Maintenant recréer
git tag v1.0.38
git push origin v1.0.38
```

#### "Permission refusée" lors de la création de release
1. Allez dans Settings > Actions > General
2. Activez "Read and write permissions"
3. Relancez le workflow

#### Le build échoue avec "dotnet not found"
- Assurez-vous que l'étape `setup-dotnet` est avant les étapes de build
- Vérifiez que `dotnet-version` correspond à votre projet

#### Artefacts non trouvés dans la release
- Vérifiez le `path` dans l'étape `upload-artifact`
- Vérifiez que les fichiers existent avec une étape `ls` ou `dir`
- Vérifiez les chemins de fichiers dans `action-gh-release`

#### Le workflow ne se déclenche pas au push du tag
- Assurez-vous que le tag correspond au pattern (ex: `v*`)
- Vérifiez la syntaxe du fichier workflow avec un validateur YAML
- Vérifiez que le workflow est sur la branche par défaut

### Conseils de Débogage

1. **Ajouter une sortie de debug :**
   ```yaml
   - name: Info de debug
     run: |
       echo "Répertoire actuel : $(pwd)"
       echo "Fichiers :"
       ls -la
   ```

2. **Activer les logs de debug :**
   - Allez dans Settings > Secrets > Actions
   - Ajoutez le secret `ACTIONS_STEP_DEBUG` = `true`

3. **Tester localement avec act :**
   ```bash
   # Installer act
   # Exécuter le workflow localement
   act push --tag v1.0.0
   ```

---

## 9. Conseils de Personnalisation

### Pour Différents Langages/Frameworks

**Node.js :**
```yaml
- uses: actions/setup-node@v4
  with:
    node-version: '20'
- run: npm ci
- run: npm run build
```

**Python :**
```yaml
- uses: actions/setup-python@v5
  with:
    python-version: '3.11'
- run: pip install -r requirements.txt
- run: python setup.py build
```

**Rust :**
```yaml
- uses: actions-rs/toolchain@v1
  with:
    toolchain: stable
- run: cargo build --release
```

### Ajouter des Tests Avant la Release

```yaml
- name: Exécuter les tests
  run: dotnet test --configuration Release --no-build

# Continuer seulement si les tests passent
- name: Publier
  if: success()
  run: dotnet publish ...
```

### Builds Multi-Plateformes

```yaml
jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      # ... étapes de build
```

### Notifications

**Discord :**
```yaml
- name: Notification Discord
  uses: sarisia/actions-status-discord@v1
  if: always()
  with:
    webhook: ${{ secrets.DISCORD_WEBHOOK }}
```

**Slack :**
```yaml
- name: Notification Slack
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Résumé

Vous avez maintenant tout ce qu'il faut pour :

1. **Comprendre** le fonctionnement de GitHub Actions
2. **Configurer** un workflow de release automatisé
3. **Utiliser** l'outil GUI Python pour des releases faciles
4. **Personnaliser** le workflow selon vos besoins
5. **Dépanner** les problèmes courants

Bonnes releases !

---

*Généré pour le projet MouseEffects - https://github.com/ltrudu/MouseEffects*
