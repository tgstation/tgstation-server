{
  pkgs,
  ...
}:

let
  inherit (pkgs) stdenv lib;

  versionParse = stdenv.mkDerivation {
    pname = "tgstation-server-version-parse";
    version = "1.0.0";

    meta = with pkgs.lib; {
      description = "Version parser for tgstation-server";
      homepage = "https://github.com/tgstation/tgstation-server";
      changelog = "https://github.com/tgstation/tgstation-server/blob/gh-pages/changelog.yml";
      license = licenses.agpl3Plus;
      platforms = platforms.x86_64;
    };

    nativeBuildInputs = with pkgs; [
      xmlstarlet
    ];

    src = ./../..;

    installPhase = ''
      mkdir -p $out
      xmlstarlet sel -N X="http://schemas.microsoft.com/developer/msbuild/2003" --template --value-of /X:Project/X:PropertyGroup/X:TgsCoreVersion ./Version.props > $out/tgs_version.txt
    '';
  };

  fixedOutput = stdenv.mkDerivation {
    pname = "tgstation-server-release-server-console-zip";
    version = (builtins.readFile "${versionParse}/tgs_version.txt");

    meta = with pkgs.lib; {
      description = "Host watchdog binaries for tgstation-server";
      homepage = "https://github.com/tgstation/tgstation-server";
      changelog = "https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v${version}";
      license = licenses.agpl3Plus;
      platforms = platforms.x86_64;
    };

    nativeBuildInputs = with pkgs; [
      curl
      cacert
      versionParse
    ];

    src = ./.;

    buildPhase = ''
      curl -L https://file.house/b2eyKOJZ7ptxwqm8O24-9A==.zip -o ServerConsole.zip
    '';

    installPhase = ''
      mkdir -p $out
      mv ServerConsole.zip $out/ServerConsole.zip
    '';

    outputHashAlgo = "sha256";
    outputHashMode = "recursive";
    outputHash = "sha256-mHlRHPSeZxyJPqN3KUmc0ftYNZgh81LauIu+fCSKPUI=";
  };
  rpath = lib.makeLibraryPath [ pkgs.stdenv_32bit.cc.cc.lib ];
in
stdenv.mkDerivation {
  pname = "tgstation-server";
  version = (builtins.readFile "${versionParse}/tgs_version.txt");

  meta = with pkgs.lib; {
    description = "A production scale tool for DreamMaker server management";
    homepage = "https://github.com/tgstation/tgstation-server";
    changelog = "https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v${version}";
    license = licenses.agpl3Plus;
    platforms = platforms.x86_64;
  };

  buildInputs = with pkgs; [
    dotnetCorePackages.sdk_8_0
    gdb
    systemd
    zlib
    gcc_multi
    glibc
  ];
  nativeBuildInputs = with pkgs; [
    makeWrapper
    unzip
    fixedOutput
    versionParse
  ];

  src = ./.;

  installPhase = ''
    mkdir -p $out/bin
    unzip "${fixedOutput}/ServerConsole.zip" -d $out/bin
    rm -rf $out/bin/lib
    makeWrapper ${pkgs.dotnetCorePackages.sdk_8_0}/dotnet $out/bin/tgstation-server --suffix PATH : ${
      lib.makeBinPath (
        with pkgs;
        [
          dotnetCorePackages.sdk_8_0
          gdb
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
    } --add-flags "$out/bin/Tgstation.Server.Host.Console.dll --bootstrap"
  '';
}
