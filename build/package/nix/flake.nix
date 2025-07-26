{
    description = "tgstation-server";

    inputs = {};

    outputs = { ... }: {
        nixosModules = {
            default = { ... }: {
                imports = [ ./tgstation-server.nix ];
            };
        };
        checks.x86_64-linux.package-build = pkgs.callPackage ./package.nix { }
    };
}
