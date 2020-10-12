import { Components } from "../generatedcode/_generated";
import { ICredentials } from "../models/ICredentials";

export default new (class CredentialsProvider {
    //token
    public token?: Components.Schemas.Token;

    //credentials
    public credentials?: ICredentials;

    public isTokenValid() {
        return (
            this.credentials &&
            this.token &&
            this.token
                .bearer /* &&
            (!this.token.expiresAt || new Date(this.token.expiresAt) > new Date(Date.now()))*/
        );
    }
})();
