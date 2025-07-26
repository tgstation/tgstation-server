{
    description = "tgstation-server";

    inputs = {};

    outputs = { nixpkgs, ... }: {
        nixosModules = {
            default = { ... }: {
                imports = [ ./tgstation-server.nix ];
            };
        };
        checks.x86_64-linux.package-build = nixpkgs.legacyPackages.x86_64-linux.callPackage ./package.nix { };
    };
}
