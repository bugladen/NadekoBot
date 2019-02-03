#!/bin/sh

DATA=/app/data

# https://gitlab.com/Kwoth/nadekobot/commit/835b2276145435f70d516913d87708f4e935cd54
[ -f "$DATA/pokemon/pokemon_abilities7.json" ] && [ ! -e "$DATA/pokemon/pokemon_abilities.json" ] && \
    mv "$DATA/pokemon/pokemon_abilities7.json" "$DATA/pokemon/pokemon_abilities.json"

[ -f "$DATA/pokemon/pokemon_list7.json" ] && [ ! -e "$DATA/pokemon/pokemon_list.json" ] && \
    mv "$DATA/pokemon/pokemon_list7.json" "$DATA/pokemon/pokemon_list.json"

[ -f "$DATA/pokemon/name-id_map4.json" ] && [ ! -e "$DATA/pokemon/name-id_map.json" ] && \
    mv "$DATA/pokemon/name-id_map4.json" "$DATA/pokemon/name-id_map.json"

# https://gitlab.com/Kwoth/nadekobot/commit/93ba400c5ba0732bf2b3906a7098bb1880c0f748
[ -f "$DATA/hangman3.json" ] && [ ! -e "$DATA/hangman.json" ] && \
    mv "$DATA/hangman3.json" "$DATA/hangman.json"

rsync -rv --ignore-existing $DATA-default/ $DATA/

exec dotnet /app/NadekoBot.dll