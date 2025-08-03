#!/bin/sh

gen_dep () {
    nuget-to-json ../../../src/$1 > deps/$1.json
}

gen_dep "Tgstation.Server.Common"
gen_dep "Tgstation.Server.Shared"
gen_dep "Tgstation.Server.Api"
gen_dep "Tgstation.Server.Host.Utils.GitLab.GraphQL"
gen_dep "Tgstation.Server.Host"
gen_dep "Tgstation.Server.Host.Watchdog"
gen_dep "Tgstation.Server.Host.Console"
