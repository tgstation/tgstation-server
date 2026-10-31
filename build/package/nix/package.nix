{
  pkgs,
  ...
}:

let
  inherit (pkgs) stdenv lib;

  versionProps = builtins.readFile ./../../Version.props;
  versionProp = name: builtins.head (builtins.match ".*<${name}>([^<]+)</${name}>.*" versionProps);

  tgstation-server-host-console = pkgs.buildDotnetModule {
    pname = "Tgstation.Server.Host.Console";
    version = versionProp "TgsHostWatchdogVersion"; # Be careful, this influences the assembly version

    src = ./../../..;

    projectFile = "src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj";
    nugetDeps = ./deps.json; # see "Generating and updating NuGet dependencies" section for details

    TGS_NIX_BUILD = "yes";

    executables = [];

    dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
    dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;
  };
in
stdenv.mkDerivation {
  pname = "tgstation-server";
  version = versionProp "TgsCoreVersion";

  meta = with pkgs.lib; {
    description = "A production scale tool for DreamMaker server management";
    homepage = "https://github.com/tgstation/tgstation-server";
    changelog = "https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v${version}";
    license = licenses.agpl3Plus;
    platforms = platforms.x86_64;
  };

  buildInputs = with pkgs; [
    pkgs.dotnetCorePackages.sdk_8_0
    gdb
    systemd
    zlib
    gcc_multi
    glibc
    bash
    tgstation-server-host-console
  ];
  nativeBuildInputs = with pkgs; [
    makeWrapper
  ];

  src = ./.;

  installPhase = ''
    mkdir -p $out/bin
    makeWrapper ${pkgs.dotnetCorePackages.sdk_8_0}/bin/dotnet $out/bin/tgstation-server --suffix PATH : ${
      lib.makeBinPath (
        with pkgs;
        [
          pkgs.dotnetCorePackages.sdk_8_0
          gdb
          bash
        ]
      )
    } --suffix LD_LIBRARY_PATH : ${
      lib.makeLibraryPath (
        with pkgs;
        [
          systemd
          zlib
        ]
      )
    } --add-flags "${tgstation-server-host-console}/lib/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.dll --bootstrap"
  '';
}
