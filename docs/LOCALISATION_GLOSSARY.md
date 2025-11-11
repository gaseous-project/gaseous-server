# Localisation Glossary (EN ↔ FR ↔ DE ↔ PT)

This glossary standardises high‑frequency UI terms across English (EN), French (FR), German (DE) and Portuguese (PT). Use it to ensure consistency, especially when adding new locales or reviewing overlays.

## Core UI Terms
<!-- GLOSSARY_TABLE_START -->
| Key Concept | English | French | German | Portuguese |
|-------------|---------|--------|--------|------------|
| settings | Settings | Paramètres | Einstellungen | Definições |
| preferences | Preferences | Préférences | Voreinstellungen | Preferências |
| upload | Upload | Importer | Hochladen | Carregar |
| download | Download | — | Herunterladen | Transferir |
| library | Library | Bibliothèque | Bibliothek | Biblioteca |
| home | Home | Accueil | Startseite | Início |
| favorites | Favorites | Favoris | Favoriten | Favoritos |
| play | Play | Jouer | Spielen | Jogar |
| delete | Delete | Supprimer | Löschen | Eliminar |
| add | Add | Ajouter | Hinzufügen | Adicionar |
| emulator | Emulator | Émulateur | Emulator | Emulador |
| firmware | Firmware | Firmware | Firmware | Firmware |
| metadata | Metadata | Métadonnées | Metadaten | Metadados |
| signature | Signature Source | Source de signature | Signaturquelle | Fonte de Assinatura |
| rom | ROMs | ROM | ROMs | ROMs |
| media | Media: {0} | Support : {0} | Medien: {0} | Média: {0} |
| screenshot | Screenshot | Capture d'écran | Screenshot | Captura |
| saved /game | Saved Games | Jeux sauvegardés | Gespeicherte Spiele | Jogos Gravados |
| user | New User | Nouvel utilisateur | Neuer Benutzer | Novo Utilizador |
| username | Username | Nom d'utilisateur | Benutzername | Nome de Utilizador |
| password | Current Password | Mot de passe actuel | Aktuelles Passwort | Palavra-passe actual |
| two /factor /authentication | Two Factor Authentication | Authentification à deux facteurs | Zwei-Faktor-Authentifizierung | Autenticação de Dois Factores |
| error | Error | Erreur : {0} | Fehler | Erro |
| warning | ⚠️ Warning | — | ⚠️ Warnung | ⚠️ Aviso |
| information | ℹ️ Information | — | ℹ️ Information | ℹ️ Informação |
| search | Title Search | — | Titelsuche | Pesquisa por Título |
| filter | Filter | Filtre | Filter | Filtro |
| platform | Platforms | Plateformes | Plattformen | Plataformas |
| genre | Genres | Genres | Genres | Géneros |
| theme | Themes | Thèmes | Themen | Temas |
| rating | Rating: {0} | Note : {0} | Bewertung: {0} | Classificação: {0} |
| player | Player | Joueur | Spieler | Player |
| collection | Collection | Collection | Sammlung | Colecção |
| size | Size: {0} | Taille : {0} | Größe: {0} | Tamanho: {0} |
| summary | Summary | Résumé | Zusammenfassung | Resumo |
| description | Description | Description  | Beschreibung | Descrição |
| link | Link | Lien  | Link | Ligação |
| source | Source | Source | Quelle | Fonte |
| logs | Logs | Journaux | Protokolle | Registos |
| task | Library scan | — | Bibliothek-Scan | Análise à biblioteca |
| maintenance | Weekly maintenance | — | Wöchentliche Wartung | Manutenção semanal |
| database | Database | Base de données | Datenbank | Base de Dados |
| import | Upload | Importer | Hochladen | Carregar |
| export | Export to JSON | Exporter en JSON | Als JSON exportieren | Exportar para JSON |
| reset | Reset to Default | Réinitialiser par défaut | Auf Standard zurücksetzen | Repor Padrão |
| background | Background | Arrière-plan | Hintergrund | Segundo Plano |
| enabled | Enabled | Activé | Aktiv | Activo |
| disabled | Disabled | Désactivé | Inaktiv | Inactivo |
| pending | Pending | En attente | Ausstehend | Pendente |
| processing | Processing | Traitement | Verarbeite | A processar |
| complete | Complete | Terminé | Abgeschlossen | Concluído |
| failed | Failed | Échec | Fehlgeschlagen | Falhou |
| session | Failed to create session: {0} {1} | Échec de la création de la session : {0} {1} | Sitzung konnte nicht erstellt werden: {0} {1} | Falha ao criar sessão: {0} {1} |
| game | Internet Game Database (Recommended) | Gaseous | Internet Game Database (Empfohlen) | Internet Game Database (Recomendado) |
| cover /art | Coverart | Illustration | Coverart | Arte da Capa |
<!-- GLOSSARY_TABLE_END -->
## Style & Lexical Guidance
- French Upload: Use "Importer" uniformly (avoid regional variants like "Téléverser" for consistency).
- Portuguese Variants: Base (pt) uses European forms (Definições, Carregar, Transferir). Overlay `pt-BR` adjusts: Settings → Configurações; Upload → Enviar; Download → Baixar.
- German Compound Nouns: Prefer full forms (Benutzerverwaltung, Datenbank-Schema-Version). Avoid mixing English loanwords unless UI standard (e.g., "Reset" already translated as "Zurücksetzen").
- Title Capitalisation: Follow native language norms (sentence case for FR/DE/PT, Title Case optional only in EN where already used).
- Accents & Diacritics: Ensure proper usage (e.g., "État", "Über", "Configurações", "Colecção"). Watch for encoding in JSON (UTF–8).
- Consistent Hyphenation: English uses hyphen in "Two-Factor"; in German use "Zwei-Faktor-Authentifizierung"; Portuguese uses "dois fatores" (no hyphen) and French "à deux facteurs".

## Pluralisation Patterns (Summary)
- English: {one, other} plus extended custom set internally (zero/few/many/other) – UI strings mostly one/other.
- French: one (n==0 || n==1), other (n>1).
- German & Portuguese: one (n==1), other (n!=1).
- Dutch: one (n==1), other (n!=1).
- Japanese: other (n is any number; no plural distinction).
- Russian: one (n%10==1 && n%100!=11), few (n%10 in 2..4 && n%100 not in 12..14), many (n%10==0 || n%10 in 5..9 || n%100 in 11..14), other (everything else).

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



