#!/bin/bash

echo "----------------------------------------------------------------------------------------------------" >> /tmp/tgs_wrap_gpg_output.log
echo "Original gpg args: \"$@\"" >> /tmp/tgs_wrap_gpg_output.log

# S/O copypasta
# https://stackoverflow.com/questions/9970212/how-do-i-find-the-last-positional-parameter-in-linux
# https://stackoverflow.com/questions/192249/how-do-i-parse-command-line-arguments-in-bash
DSC_PATH=${!#}

POSITIONAL_ARGS=()

while [[ $# -gt 0 ]]; do
  case $1 in
    -o|--output)
      GPG_OUTPUT="$2"
      shift # past argument
      shift # past value
      ;;
    --clear-sign|--clearsign)
      $DSC_PATH="$2"
      shift # past argument
      shift # past value
      ;;
    *)
      POSITIONAL_ARGS+=("$1") # save positional arg
      shift # past argument
      ;;
  esac
done

set -- "${POSITIONAL_ARGS[@]}" # restore positional parameters

echo "Running gpg with args '--batch --yes --pinentry-mode loopback --passphrase *** --weak-digest SHA1 --weak-digest RIPEMD160 --utf8-strings --armor --openpgp --personal-digest-preferences \"SHA512 SHA384 SHA256 SHA224\" --output $GPG_OUTPUT --clearsign $DSC_PATH'" >> /tmp/tgs_wrap_gpg_output.log

if [[ -f "$GPG_OUTPUT" ]]; then
    echo "$GPG_OUTPUT exists." >> /tmp/tgs_wrap_gpg_output.log
else
    echo "$GPG_OUTPUT DOESN'T EXIST! This is to be expected." >> /tmp/tgs_wrap_gpg_output.log
fi

if [[ -f "$DSC_PATH" ]]; then
    echo "$DSC_PATH exists." >> /tmp/tgs_wrap_gpg_output.log
else
    echo "$DSC_PATH DOESN'T EXIST!" >> /tmp/tgs_wrap_gpg_output.log
fi

echo ">>>>" >> /tmp/tgs_wrap_gpg_output.log
gpg --batch --yes --pinentry-mode loopback --passphrase "$PACKAGING_PRIVATE_KEY_PASSPHRASE" --weak-digest SHA1 --weak-digest RIPEMD160 --utf8-strings --armor --openpgp --personal-digest-preferences "SHA512 SHA384 SHA256 SHA224" --output "$GPG_OUTPUT" --clearsign "$DSC_PATH"  >> /tmp/tgs_wrap_gpg_output.log
GPG_EXIT_CODE=$?

echo ">>>>" >> /tmp/tgs_wrap_gpg_output.log

if [[ -f "$GPG_OUTPUT" ]]; then
    echo "$GPG_OUTPUT exists." >> /tmp/tgs_wrap_gpg_output.log
else
    echo "$GPG_OUTPUT DOESN'T EXIST AFTER IT WAS SUPPOSED TO BE CREATED WTF!" >> /tmp/tgs_wrap_gpg_output.log
fi

exit $GPG_EXIT_CODE
