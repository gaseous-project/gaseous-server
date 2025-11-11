# Localisation Glossary (EN ↔ FR ↔ DE ↔ PT)

This glossary standardises high‑frequency UI terms across English (EN), French (FR), German (DE) and Portuguese (PT). Use it to ensure consistency, especially when adding new locales or reviewing overlays.

## Core UI Terms
| Key Concept | English | French | German | Portuguese |
|-------------|---------|--------|--------|------------|
| Settings | Settings | Paramètres | Einstellungen | Definições |
| Preferences | Preferences | Préférences | Voreinstellungen | Preferências |
| Upload (generic verb) | Upload | Importer (avoid Téléverser) | Hochladen | Carregar (pt-PT neutral) / Enviar (pt-BR overlay) |
| Download | Download | Télécharger | Herunterladen | Transferir (preferred) |
| Library | Library | Bibliothèque | Bibliothek | Biblioteca |
| Home | Home | Accueil | Startseite | Início |
| Favorites | Favorites | Favoris | Favoriten | Favoritos |
| Play | Play | Jouer | Spielen | Jogar |
| Delete | Delete | Supprimer | Löschen | Eliminar |
| Add | Add | Ajouter | Hinzufügen | Adicionar |
| Emulator | Emulator | Émulateur | Emulator | Emulador |
| Firmware | Firmware | Firmware | Firmware | Firmware |
| Metadata | Metadata | Métadonnées | Metadaten | Metadados |
| Signature (hash match) | Signature | Signature | Signatur | Assinatura |
| ROM | ROM | ROM | ROM | ROM |
| Media | Media | Support | Medien | Média |
| Screenshot | Screenshot | Capture d'écran | Screenshot | Captura |
| Saved Game / Save State | Saved Game / Save State | Jeu sauvegardé / État sauvegardé | Gespeichertes Spiel / Spielstand | Jogo gravado / Estado gravado |
| User | User | Utilisateur | Benutzer | Utilizador |
| Username | Username | Nom d'utilisateur | Benutzername | Nome de utilizador |
| Password | Password | Mot de passe | Passwort | Palavra-passe |
| Two-Factor Authentication | Two-Factor Authentication | Authentification à deux facteurs | Zwei-Faktor-Authentifizierung | Autenticação de dois fatores |
| Error | Error | Erreur | Fehler | Erro |
| Warning | Warning | Avertissement | Warnung | Aviso |
| Information | Information | Information | Information | Informação |
| Search | Search | Recherche | Suche | Pesquisa |
| Filter | Filter | Filtre | Filter | Filtro |
| Platform | Platform | Plateforme | Plattform | Plataforma |
| Genre | Genre | Genre | Genre | Género |
| Theme | Theme | Thème | Thema | Tema |
| Rating | Rating | Note | Bewertung | Classificação |
| Player | Player | Joueur | Spieler | Jogador |
| Collection | Collection | Collection | Sammlung | Colecção |
| Size | Size | Taille | Größe | Tamanho |
| Summary | Summary | Résumé | Zusammenfassung | Resumo |
| Description | Description | Description | Beschreibung | Descrição |
| Link | Link | Lien | Link | Ligação |
| Source | Source | Source | Quelle | Fonte |
| Logs | Logs | Journaux | Protokolle | Registos |
| Task | Task | Tâche | Aufgabe | Tarefa |
| Maintenance | Maintenance | Maintenance | Wartung | Manutenção |
| Database | Database | Base de données | Datenbank | Base de Dados |
| Import | Import | Importer | Importieren | Importar |
| Export | Export | Exporter | Exportieren | Exportar |
| Reset (to default) | Reset | Réinitialiser | Zurücksetzen | Repor |
| Background (process) | Background | Arrière-plan | Hintergrund | Segundo plano |
| Enabled | Enabled | Activé | Aktiv | Activo |
| Disabled | Disabled | Désactivé | Inaktiv | Inactivo |
| Pending | Pending | En attente | Ausstehend | Pendente |
| Processing | Processing | Traitement | Verarbeite / Verarbeitung läuft | A processar |
| Complete | Complete | Terminé | Abgeschlossen | Concluído |
| Failed | Failed | Échec | Fehlgeschlagen | Falhou |
| Session | Session | Session | Sitzung | Sessão |
| Game | Game | Jeu | Spiel | Jogo |
| Cover Art | Coverart | Illustration de couverture | Coverart / Titelbild | Arte da capa |

## Style & Lexical Guidance
- French Upload: Use "Importer" uniformly (avoid regional variants like "Téléverser" for consistency).
- Portuguese Variants: Base (pt) uses European forms (Definições, Carregar, Transferir). Overlay `pt-BR` adjusts: Settings → Configurações; Upload → Enviar; Download → Baixar.
- German Compound Nouns: Prefer full forms (Benutzerverwaltung, Datenbank-Schema-Version). Avoid mixing English loanwords unless UI standard (e.g., "Reset" already translated as "Zurücksetzen").
- Title Capitalisation: Follow native language norms (sentence case for FR/DE/PT, Title Case optional only in EN where already used).
- Accents & Diacritics: Ensure proper usage (e.g., "État", "Über", "Configurações", "Colecção"). Watch for encoding in JSON (UTF‑8).
- Consistent Hyphenation: English uses hyphen in "Two-Factor"; in German use "Zwei-Faktor-Authentifizierung"; Portuguese uses "dois fatores" (no hyphen) and French "à deux facteurs".

## Pluralisation Patterns (Summary)
- English: {one, other} plus extended custom set internally (zero/few/many/other) – UI strings mostly one/other.
- French: one (n==0 || n==1), other (n>1).
- German & Portuguese: one (n==1), other (n!=1).

## Term Decision Log (Rationale)
- "Upload" harmonised to verbs (Import/Importer | Hochladen | Carregar/Enviar) instead of noun forms for action buttons.
- "Reset to Default" chosen over "Restore Defaults" for brevity; mapped to natural verbal forms in FR/DE/PT.
- "Favorites" vs "Favourites": Base EN keeps American spelling; overlay like `en-AU` may override—glossary records concept not spelling variant; FR/DE/PT unaffected.

## Adding New Languages
When introducing a new locale:
1. Copy base English keys; translate using glossary for existing concepts before new creative choices.
2. Check plural rules via CLDR; add minimal forms (one/other) unless language demands more.
3. Preserve JSON key order for diff clarity (optional but recommended).
4. Run a key parity diff script (future tooling) to confirm no omissions.
5. Add overlay files only for genuine regional lexical differences.

## Reserved / Do Not Translate
- Project name: "Gaseous"
- External service names: Hasheous, IGDB, EmulatorJS, Progetto-Snaps, No-Intro, Redump, WHDLoad, Retro Achievements.
- Hash algorithms: MD5, SHA1, SHA256, CRC32.

## Future Improvements
- Automate glossary extraction from base locales.
- Include context notes (e.g., "Signature" = hash match, not handwritten).
- Flag culturally sensitive terms for review (age ratings).

---
Last updated: 2025-11-11
