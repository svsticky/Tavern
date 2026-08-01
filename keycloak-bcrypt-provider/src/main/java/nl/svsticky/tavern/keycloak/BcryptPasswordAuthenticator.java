package nl.svsticky.tavern.keycloak;

import jakarta.ws.rs.core.MultivaluedMap;
import jakarta.ws.rs.core.Response;
import org.keycloak.authentication.AuthenticationFlowContext;
import org.keycloak.authentication.AuthenticationFlowError;
import org.keycloak.authentication.Authenticator;
import org.keycloak.events.Details;
import org.keycloak.events.Errors;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserCredentialModel;
import org.keycloak.models.UserModel;
import org.keycloak.representations.idm.CredentialRepresentation;
import org.keycloak.services.managers.AuthenticationManager;
import org.mindrot.jbcrypt.BCrypt;

public class BcryptPasswordAuthenticator implements Authenticator {

    static final String LEGACY_BCRYPT_HASH_ATTRIBUTE = "legacy_bcrypt_hash";

    @Override
    public void authenticate(AuthenticationFlowContext context) {
        Response challenge = context.form().createLoginUsernamePassword();
        context.challenge(challenge);
    }

    @Override
    public void action(AuthenticationFlowContext context) {
        MultivaluedMap<String, String> formData =
                context.getHttpRequest().getDecodedFormParameters();

        if (formData.containsKey("cancel")) {
            context.cancelLogin();
            return;
        }

        String username = formData.getFirst(AuthenticationManager.FORM_USERNAME);
        String password = formData.getFirst(CredentialRepresentation.PASSWORD);

        if (username == null || username.isBlank()) {
            Response challenge = context.form()
                    .setError("missingUsername")
                    .createLoginUsernamePassword();
            context.failureChallenge(AuthenticationFlowError.INVALID_USER, challenge);
            return;
        }

        if (password == null || password.isEmpty()) {
            Response challenge = context.form()
                    .setError("missingPassword")
                    .createLoginUsernamePassword();
            context.failureChallenge(AuthenticationFlowError.INVALID_CREDENTIALS, challenge);
            return;
        }

        context.getEvent().detail(Details.USERNAME, username);
        context.getAuthenticationSession().setAuthNote(
                AuthenticationManager.FORM_USERNAME, username);

        // Look up user by username then email
        UserModel user = context.getSession().users()
                .getUserByUsername(context.getRealm(), username);
        if (user == null) {
            user = context.getSession().users()
                    .getUserByEmail(context.getRealm(), username);
        }

        if (user == null) {
            context.getEvent().error(Errors.USER_NOT_FOUND);
            Response challenge = context.form()
                    .setError("invalidUsernameOrPassword")
                    .createLoginUsernamePassword();
            context.failureChallenge(AuthenticationFlowError.INVALID_USER, challenge);
            return;
        }

        context.setUser(user);

        if (!user.isEnabled()) {
            context.getEvent().error(Errors.USER_DISABLED);
            Response challenge = context.form()
                    .setError("accountDisabledError")
                    .createLoginUsernamePassword();
            context.failureChallenge(AuthenticationFlowError.USER_DISABLED, challenge);
            return;
        }

        String bcryptHash = user.getFirstAttribute(LEGACY_BCRYPT_HASH_ATTRIBUTE);

        if (bcryptHash != null) {
            boolean hashMatches;
            try {
                hashMatches = BCrypt.checkpw(password, bcryptHash);
            } catch (Exception e) {
                // Malformed hash stored — treat as wrong password
                hashMatches = false;
            }

            if (hashMatches) {
                // Migrate to PBKDF2 and remove the legacy attribute
                user.credentialManager().updateCredential(
                        UserCredentialModel.password(password, false));
                user.removeAttribute(LEGACY_BCRYPT_HASH_ATTRIBUTE);
                context.success();
            } else {
                context.getEvent().error(Errors.INVALID_USER_CREDENTIALS);
                invalidPassword(context);
            }
        } else {
            // No legacy hash — standard PBKDF2 check
            boolean valid = user.credentialManager()
                    .isValid(UserCredentialModel.password(password));
            if (valid) {
                context.success();
            } else {
                context.getEvent().error(Errors.INVALID_USER_CREDENTIALS);
                invalidPassword(context);
            }
        }
    }

    private void invalidPassword(AuthenticationFlowContext context) {
        Response challenge = context.form()
                .setError("invalidUsernameOrPassword")
                .createLoginUsernamePassword();
        context.failureChallenge(AuthenticationFlowError.INVALID_CREDENTIALS, challenge);
    }

    @Override
    public boolean requiresUser() {
        return false;
    }

    @Override
    public boolean configuredFor(KeycloakSession session, RealmModel realm, UserModel user) {
        return true;
    }

    @Override
    public void setRequiredActions(KeycloakSession session, RealmModel realm, UserModel user) {
        // no required actions to set — the authenticator does not gate on any action
    }

    @Override
    public void close() {
        // stateless singleton; nothing to release
    }
}
