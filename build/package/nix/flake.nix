{
    description = "tgstation-server";

    inputs = {};

    outputs = { ... }: {
        nixosModules = {
            default = { ... }: {
                imports = [ ./tgstation-server.nix ];
            };
        };
    };
}
