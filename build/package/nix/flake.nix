{
    description = "tgstation-server";

    inputs = {};

    outputs = { ... }: {
        nixosModules = {
            default = { ... }: {
                imports = [ ./tgstation-server.nix ];
            };
        };
        checks.x86_64-linux.flake-build = self.packages.x86_64-linux.default;
    };
}
