#!/bin/bash -e

base="http://localhost:8096"
token=13768c5f8e14451e934e3cd0a60af077
plugin="КиноПоиск (белый список)"

call() {
    headers=(
        -H 'Authorization: MediaBrowser Token='$token''
        -H 'Content-Type: application/json'
    )
    (
        # set -x
        curl -s "${headers[@]}" $@
    )
}
get() {
    call "$base/$1" ${@:2}
}
post() {
    call -X POST "$base/$1" ${@:2}
}

restart() {
    printf "Restarting Jellyfin Server"
    post System/Restart
    while true; do
        printf '.'
        sleep 1
        pending=$(get System/Info | jq .HasPendingRestart? 2>/dev/null)
        [ "$pending" == "false" ] && break
    done
    echo OK
}

reload() {
    printf "Reloading Library"
    params=(
        Recursive=true
        ReplaceAllMetadata=true
        MetadataRefreshMode=FullRefresh
    )
    params=$(echo "${params[@]}" | tr ' ' '&')
    items=$(get Library/VirtualFolders | jq -r .[].ItemId)
    for item in $items; do
        printf '.'
        post "Items/$item/Refresh?$params"
    done

    while true; do
        printf '.'
        status=$(get Library/VirtualFolders | jq -r .[].RefreshStatus)
        for line in ${status[@]}; do
            [ "$line" != "Idle" ] && {
                sleep 1
                continue 2
            }
        done
        break
    done

    echo OK
}

list() {
    params=(
        IncludeItemTypes=Movie
        Recursive=true
        Fields=AllFields
    )
    params=$(echo "${params[@]}" | tr ' ' '&')
    get "Items?$params" | jq '.Items[]? | { Name, ProductionYear, CriticRating, CommunityRating }'
}

checkPlugin() {
    read -ra info <<< $(get "Plugins" \
        | jq -r '.[] | select(.Name == "'"$plugin"'") | .Id, .Status, .Version')

    id=${info[0]}
    status=${info[1]}
    version=${info[2]}

    [ "$id" ] || {
        get Plugins | jq .
        echo "Plugin not found!";
        exit 1
    }
    echo Plugin $plugin: $status
    case $status in
        Active)
            ;;
        Disabled)
            post "Plugins/$id/$version/Enable"
            sleep 1
            ;;
        *)
            exit 1
            ;;
    esac
}



[ "$1" == "restart" ] && restart
checkPlugin
reload
list
