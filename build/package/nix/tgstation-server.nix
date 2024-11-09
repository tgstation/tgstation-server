inputs@{
  config,
  lib,
  nixpkgs,
  pkgs,
  ...
}:

let
  pkgs-i686 = nixpkgs.legacyPackages.i686-linux;

  cfg = config.services.tgstation-server;

  package = import ./package.nix inputs;

  stdenv = pkgs-i686.stdenv_32bit;

  rpath = pkgs-i686.lib.makeLibraryPath [
    stdenv.cc.cc.lib
  ];

  byond-patcher = pkgs-i686.writeShellScriptBin "byond-patcher" ''
    BYOND_PATH=$(realpath ../../Byond/$1/byond/bin/)

    patchelf --set-interpreter "$(cat ${stdenv.cc}/nix-support/dynamic-linker)" \
      --set-rpath "$BYOND_PATH:${rpath}" \
      $BYOND_PATH/{DreamDaemon,DreamDownload,DreamMaker}
  '';
in
{
  ##### interface. here we define the options that users of our service can specify
  options = {
    # the options for our service will be located under services.foo
    services.tgstation-server = {
      enable = lib.mkOption {
        type = lib.types.bool;
        default = false;
        description = ''
          Whether to enable tgstation-server.
        '';
      };

      username = lib.mkOption {
        type = lib.types.str;
        default = "tgstation-server";
        description = ''
          The name of the user used to execute tgstation-server.
        '';
      };

      groupname = lib.mkOption {
        type = lib.types.str;
        default = "tgstation-server";
        description = ''
          The name of group the user used to execute tgstation-server will belong to.
        '';
      };

      home-directory = lib.mkOption {
        type = lib.types.str;
        default = "/home/tgstation-server";
        description = ''
          The home directory of TGS. Should be persistent.
        '';
      };

      production-appsettings = lib.mkOption {
        type = lib.types.lines;
        default = '''';
        description = ''
          The contents of appsettings.Production.yml in the /etc/tgstation-server.d directory.
        '';
      };
    };
  };

  config = lib.mkIf cfg.enable {
    users.groups."${cfg.groupname}" = { };

    users.users."${cfg.username}" = {
      isSystemUser = true;
      createHome = true;
      group = cfg.groupname;
      home = cfg.home-directory;
    };

    environment.etc = {
      "tgstation-server.d/appsettings.yml" = {
        text = (builtins.readFile "${package}/bin/appsettings.yml");
        group = cfg.groupname;
        mode = "0644";
      };
      "tgstation-server.d/appsettings.Production.yml" = {
        text = cfg.production-appsettings;
        group = cfg.groupname;
        mode = "0640";
      };
      "tgstation-server.d/EventScripts/EngineInstallComplete-050-PatchELFByond.sh" = {
        source = "${byond-patcher}/bin/byond-patcher";
        group = cfg.groupname;
        mode = "755";
      };
    };

    systemd.services.tgstation-server = {
      description = "tgstation-server";
      serviceConfig = {
        User = cfg.username;
        Type = "notify-reload";
        NotifyAccess = "all";
        WorkingDirectory = "${package}/bin";
        ExecStart = "${package}/bin/tgstation-server --appsettings-base-path=/etc/tgstation-server.d --General:SetupWizardMode=Never --General:AdditionalEventScriptsDirectories:0=/etc/tgstation-server.d/EventScripts";
        Restart = "always";
        KillMode = "process";
        ReloadSignal = "SIGUSR2";
        AmbientCapabilities = "CAP_SYS_NICE CAP_SYS_PTRACE";
        WatchdogSec = "60";
        WatchdogSignal = "SIGTERM";
        LogsDirectory = "tgstation-server";
      };
      wantedBy = [ "multi-user.target" ];
    };
  };
}
