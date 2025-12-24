inputs@{
  config,
  lib,
  systemdUtils,
  nixpkgs,
  pkgs,
  writeShellScriptBin,
  ...
}:

let
  pkgs-i686 = nixpkgs.legacyPackages.i686-linux;

  cfg = config.services.tgstation-server;

  package = import ./package.nix inputs;

  stdenv32 = pkgs-i686.stdenv_32bit;

  curl32 = pkgs-i686.curl.override { stdenv = stdenv32; };

  rpath = pkgs-i686.lib.makeLibraryPath [
    stdenv32.cc.cc.lib
    curl32
  ];

  byond-patcher = pkgs-i686.writeShellScriptBin "EngineInstallComplete-050-TgsPatchELFByond.sh" ''
    # If the file doesn't exist, assume OD
    BYOND_BIN_PATH="''${TGS_INSTANCE_ROOT}/Byond/$1/byond/bin/"
    if [[ ! -f "''${BYOND_BIN_PATH}DreamDaemon" ]] ; then
      echo "DreamDaemon doesn't appear to exist. Assuming OD install"
      exit
    fi

    BYOND_PATH=$(realpath $BYOND_BIN_PATH)

    ${pkgs.patchelf}/bin/patchelf --set-interpreter "$(cat ${stdenv32.cc}/nix-support/dynamic-linker)" \
      --set-rpath "$BYOND_PATH:${rpath}" \
      $BYOND_PATH/{DreamDaemon,DreamDownload,DreamMaker}
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
        type = lib.types.nonEmptyStr;
        default = "tgstation-server";
        description = ''
          The name of the user used to execute tgstation-server.
        '';
      };

      groupname = lib.mkOption {
        type = lib.types.nonEmptyStr;
        default = "tgstation-server";
        description = ''
          The name of group the user used to execute tgstation-server will belong to.
        '';
      };

      home-directory = lib.mkOption {
        type = lib.types.nonEmptyStr;
        default = "/home/tgstation-server";
        description = ''
          The home directory of TGS. Should be persistent.
        '';
      };

      production-appsettings = lib.mkOption {
        type = lib.types.path;
        default = '''';
        description = ''
          A formatted appsettings.Production.yml file.
        '';
      };

      extra-path = lib.mkOption {
        type = lib.types.str;
        default = "";
        description = ''
          Extra PATH entries to add to the TGS process
        '';
      };

      environmentFile = lib.mkOption {
        type = lib.types.nullOr lib.types.path;
        default = null;
        description = ''
         Environment file as defined in {manpage}`systemd.exec(5)`
        '';
      };

      wants = lib.mkOption {
        type = lib.types.listOf systemdUtils.lib.unitNameType;
        default = [];
        description = ''
          Start the specified units when this unit is started.
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
        source = cfg.production-appsettings;
        group = cfg.groupname;
        mode = "0640";
      };
      "tgstation-server.d/EventScripts/README.txt" = {
        text = "TGS event scripts placed here will be executed by all online instances";
        group = cfg.groupname;
        mode = "0640";
      };
    };

    systemd.services.tgstation-server = {
      description = "tgstation-server";
      serviceConfig = {
        EnvironmentFile = lib.mkIf (cfg.environmentFile != null) cfg.environmentFile;
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
      reloadTriggers = [
        (lib.mkIf (cfg.environmentFile != null) [ cfg.environmentFile ])
        "/etc/tgstation.server.d/appsettings.Production.yml"
      ];
      restartIfChanged = false; # So that the TGS service doesn't just get restarted whenever it's updated, and boots players
      wantedBy = [ "multi-user.target" ];
      wants = [ "network-online.target" ];
      after = [
        "network-online.target"
        "mysql.service"
        "mariadb.service"
        "postgresql.service"
        "mssql-server.service"
      ];
    };
  };
}
