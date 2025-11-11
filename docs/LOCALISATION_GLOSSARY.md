# Localisation Glossary (EN ↔ FR ↔ DE ↔ PT)

This glossary standardises high‑frequency UI terms across English (EN), French (FR), German (DE) and Portuguese (PT). Use it to ensure consistency, especially when adding new locales or reviewing overlays.

## Core UI Terms
<!-- GLOSSARY_TABLE_START -->
| Key Concept | English | French | German | Portuguese |
|-------------|---------|--------|--------|------------|
| settings | Settings | Paramètres | Einstellungen | Definições |\n| preferences | Preferences | Préférences | Voreinstellungen | Preferências |\n| upload | Upload | Importer | Hochladen | Carregar |\n| download | Download | — | Herunterladen | Transferir |\n| library | Library | Bibliothèque | Bibliothek | Biblioteca |\n| home | Home | Accueil | Startseite | Início |\n| favorites | Favorites | Favoris | Favoriten | Favoritos |\n| play | Play | Jouer | Spielen | Jogar |\n| delete | Delete | Supprimer | Löschen | Eliminar |\n| add | Add | Ajouter | Hinzufügen | Adicionar |\n| emulator | Emulator | Émulateur | Emulator | Emulador |\n| firmware | Firmware | Firmware | Firmware | Firmware |\n| metadata | Metadata | Métadonnées | Metadaten | Metadados |\n| signature | Signature Source | Source de signature | Signaturquelle | Fonte de Assinatura |\n| rom | ROMs | ROM | ROMs | ROMs |\n| media | Media: {0} | Support : {0} | Medien: {0} | Média: {0} |\n| screenshot | Screenshot | Capture d'écran | Screenshot | Captura |\n| saved /game | Saved Games | Jeux sauvegardés | Gespeicherte Spiele | Jogos Gravados |\n| user | New User | Nouvel utilisateur | Neuer Benutzer | Novo Utilizador |\n| username | Username | Nom d'utilisateur | Benutzername | Nome de Utilizador |\n| password | Current Password | Mot de passe actuel | Aktuelles Passwort | Palavra-passe actual |\n| two /factor /authentication | Two Factor Authentication | Authentification à deux facteurs | Zwei-Faktor-Authentifizierung | Autenticação de Dois Factores |\n| error | Error | Erreur : {0} | Fehler | Erro |\n| warning | ⚠️ Warning | — | ⚠️ Warnung | ⚠️ Aviso |\n| information | ℹ️ Information | — | ℹ️ Information | ℹ️ Informação |\n| search | Title Search | — | Titelsuche | Pesquisa por Título |\n| filter | Filter | Filtre | Filter | Filtro |\n| platform | Platforms | Plateformes | Plattformen | Plataformas |\n| genre | Genres | Genres | Genres | Géneros |\n| theme | Themes | Thèmes | Themen | Temas |\n| rating | Rating: {0} | Note : {0} | Bewertung: {0} | Classificação: {0} |\n| player | Player | Joueur | Spieler | Player |\n| collection | Collection | Collection | Sammlung | Colecção |\n| size | Size: {0} | Taille : {0} | Größe: {0} | Tamanho: {0} |\n| summary | Summary | Résumé | Zusammenfassung | Resumo |\n| description | Description | Description  | Beschreibung | Descrição |\n| link | Link | Lien  | Link | Ligação |\n| source | Source | Source | Quelle | Fonte |\n| logs | Logs | Journaux | Protokolle | Registos |\n| task | Library scan | — | Bibliothek-Scan | Análise à biblioteca |\n| maintenance | Weekly maintenance | — | Wöchentliche Wartung | Manutenção semanal |\n| database | Database | Base de données | Datenbank | Base de Dados |\n| import | Upload | Importer | Hochladen | Carregar |\n| export | Export to JSON | Exporter en JSON | Als JSON exportieren | Exportar para JSON |\n| reset | Reset to Default | Réinitialiser par défaut | Auf Standard zurücksetzen | Repor Padrão |\n| background | Background | Arrière-plan | Hintergrund | Segundo Plano |\n| enabled | Enabled | Activé | Aktiv | Activo |\n| disabled | Disabled | Désactivé | Inaktiv | Inactivo |\n| pending | Pending | En attente | Ausstehend | Pendente |\n| processing | Processing | Traitement | Verarbeite | A processar |\n| complete | Complete | Terminé | Abgeschlossen | Concluído |\n| failed | Failed | Échec | Fehlgeschlagen | Falhou |\n| session | Failed to create session: {0} {1} | Échec de la création de la session : {0} {1} | Sitzung konnte nicht erstellt werden: {0} {1} | Falha ao criar sessão: {0} {1} |\n| game | Internet Game Database (Recommended) | Gaseous | Internet Game Database (Empfohlen) | Internet Game Database (Recomendado) |\n| cover /art | Coverart | Illustration | Coverart | Arte da Capa |\n<!-- GLOSSARY_TABLE_END -->

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
