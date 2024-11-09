inputs@{
  config,
  lib,
  nixpkgs,
  pkgs,
  writeShellScriptBin,
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

  byond-patcher = pkgs-i686.writeShellScriptBin "EngineInstallComplete-050-TgsPatchELFByond.sh" ''
    BYOND_PATH=$(realpath ../../Byond/$1/byond/bin/)

    ${pkgs.patchelf}/bin/patchelf --set-interpreter "$(cat ${stdenv.cc}/nix-support/dynamic-linker)" \
      --set-rpath "$BYOND_PATH:${rpath}" \
      $BYOND_PATH/{DreamDaemon,DreamDownload,DreamMaker}

    ${pkgs.patchelf}/bin/patchelf --set-rpath "$BYOND_PATH:${rpath}" $BYOND_PATH/*.so
  '';

  tgs-wrapper = pkgs.writeShellScriptBin "tgs-path-wrapper" ''
    export PATH=$PATH:${cfg.extra-path}
    exec ${package}/bin/tgstation-server --appsettings-base-path=/etc/tgstation-server.d --General:SetupWizardMode=Never --General:AdditionalEventScriptsDirectories:0=/etc/tgstation-server.d/EventScripts --General:AdditionalEventScriptsDirectories:1=${byond-patcher}/bin
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

      extra-path = lib.mkOption {
        type = lib.types.str;
        default = "";
        description = ''
          Extra PATH entries to add to the TGS process
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
    };

    systemd.services.tgstation-server = {
      description = "tgstation-server";
      serviceConfig = {
        User = cfg.username;
        Type = "notify-reload";
        NotifyAccess = "all";
        WorkingDirectory = "${package}/bin";
        ExecStart = "${tgs-wrapper}/bin/tgs-path-wrapper";
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
